// #define CTS_FLOW_CONTROL_ON

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace StepperMotorController
{
    public partial class Form1 : Form
    {
        string OpenedFile = "";

        long LaserSensorReading;

        string buffer = "";

        bool port_busy = false; // Indicates whether or not the application is currently engaged in command communication

        bool script_running = false; // Indicated whether or not the application is currently running a script

        bool script_abort = false; // Used to abort script execution

        bool time_out = false;

        Queue<string> InQ1; // Console Queue

        Queue<string> InQ2; // Processing Queue

        private void Initialize()
        {
            InQ1 = new Queue<string>();

            InQ2 = new Queue<string>();

            Timer t1 = new Timer();

            t1.Interval = 1;

            t1.Tick += new EventHandler(Timer_1ms);

            t1.Start();

            Timer t2 = new Timer();

            t2.Interval = 10;

            t2.Tick += new EventHandler(Timer_10ms);

            t2.Start();
        }

        private double TestSpeed()
        {
            Queue<string> Test = new Queue<string>();

            for (int t = 0; t < 5; t++)
            {
                Test.Enqueue("axis3 off");
                Test.Enqueue("axis3 off");
                Test.Enqueue("axis3 +");
                Test.Enqueue("axis3 -");
                Test.Enqueue("ping");
            }

            double count = Test.Count;

            DateTime Start = DateTime.Now;

            ExecuteCommandQueue(Test);

            DateTime End = DateTime.Now;

            TimeSpan D = End - Start;

            double mSec = D.TotalMilliseconds / count;

            return mSec;

        }

        private bool ExecuteCommandQueue(Queue<string> Q)
        {
            // This function handles sending a Queue of commands to the controller.

            // It sends commands one by one and waits for a status message reply

            // which is either "OK" or "Undefined command".

            // Receiving "OK" will make the function move on to the next command

            // Receiving "Undefined command" will make the function try sending the command again

            // If no status message is received within a defined amount of time (250 ms), a timeout

            // error is generated

            if (port_busy) return false;

            if (!serialPort1.IsOpen) return false;

            port_busy = true;

            UpdateControls();

            InQ2.Clear(); // Delete all previous responses from controller

            string cmd = "";

            string response = "";

            bool cmd_reply_received;

            bool cmd_corrupted;

            time_out = false;

            Timer TO = new Timer(); // Timeout timer

            TO.Interval = 500;

            TO.Tick += new EventHandler(Signal_TimeOut);

            while (Q.Count > 0 && !script_abort && !time_out)
            {
                cmd = Q.Peek(); // Gets first command without dequeuing it

                cmd_reply_received = false;

                cmd_corrupted = true;

                TO.Stop();

                SendLineSlow(cmd); // Sends command to controller

                TO.Start(); // Starts timeout counter

                while (!cmd_reply_received && !script_abort && !time_out)
                {
                    while (InQ2.Count == 0 && !script_abort && !time_out)
                    {
                        Application.DoEvents();  // Wait till a response is received from controller
                    }

                    while (InQ2.Count > 0 && !script_abort && !time_out)
                    {
                        response = InQ2.Dequeue(); // Extract response from processing queue

                        if (response == "OK")
                        {
                            cmd_corrupted = false;

                            cmd_reply_received = true;

                            break;
                        }

                        if (response == "Undefined command")
                        {
                            cmd_corrupted = true;

                            cmd_reply_received = true;

                            break;
                        }
                    }
                }

                if (!cmd_corrupted) Q.Dequeue(); // If command was transmitted successfully, remove from queue

            }

            port_busy = false;

            UpdateControls();

            TO.Stop();

            if (time_out)
            {

                (new Form6("Error", "The controller did not respond to the last issued command.", " Please make sure that the controller is connected and running.", false)).ShowDialog();

                return false;
            }
            else
            {
                return true;
            }
        }

        void Signal_TimeOut(object sender, EventArgs e)
        {
            // This function is called after a certain amount of time if no reply message

            // is received after sending the last command.

            // It is used to trigger a timeout error

            time_out = true;
        }

        private void SerialDataReceived()
        {
            // This function reads and parses data in the application buffer

            // strings terminates with a new line character are extracted from the buffer

            // and added to InQ1 (Console Display Queue) and InQ2 (Processing Queue)

            string bytes = serialPort1.ReadExisting();

            char trace_char = '#';

            while (bytes.Contains (trace_char.ToString ()))
            {
                int p = bytes.IndexOf (trace_char);

                InQ1.Enqueue (((int) Environment.TickCount % 100).ToString ());
                
                bytes = bytes.Remove (p,1);
            }

            buffer += bytes;

            buffer.Replace("\r", "");

            while (buffer.Contains("\n"))
            {
                int index = buffer.IndexOf("\n");

                string item = buffer.Substring(0, index - 1);

                InQ1.Enqueue(item);

                InQ2.Enqueue(item);

                buffer = buffer.Substring(index + 1);
            }

        }

        private void UpdateConsole()
        {
            //return;
            while (InQ1.Count > 0)
            {
                textBox1.Text += InQ1.Dequeue() + "\r\n";

                try
                {
                    textBox1.SelectionStart = textBox1.Text.Length;

                    textBox1.ScrollToCaret();

                    textBox1.Refresh();
                }
                catch (Exception) { }
            }
        }

        private void Timer_10ms(object source,  EventArgs  e)
        {
            UpdateConsole();
        }

        private void Timer_1ms(object source, EventArgs e)
        {
            Random R = new Random();

            LaserSensorReading = R.Next();
        }

        private void SendLineSlow(string x)
        {
            // This function sends a string through the serial port character by character

            // It waits for the CTS line to toggle before sending the next character

            x = x + "\r";

            int start = Environment.TickCount;

            bool c = serialPort1.CtsHolding;

            int timeout_ms = 50; // Timeout for the CTS toggle

            for (int t = 0; t < x.Length; t++)
            {
                serialPort1.Write(x.Substring(t, 1));

                while (c == serialPort1.CtsHolding && Environment.TickCount - start < timeout_ms) ;

                c = serialPort1.CtsHolding;

                Application.DoEvents();
            }

        }

        private void RunScript()
        {
            // This function runs the currently-loaded script

            // It extracts the task object from every tree node in order

            // then converts it to the appropriate controller commands and sends it

            script_running = true;

            script_abort = false;

            UpdateControls();

            TreeNode N;

            task T;

            Font B = new Font("Tahoma", (float)8.25, FontStyle.Bold, GraphicsUnit.Point);

            Font Old = treeView1.Font;

            Queue<string> Batch = new Queue<string>();

            Batch.Enqueue("stop"); // Halts any running tasks

            Batch.Enqueue("screen 2"); // Switch to screen 2

            bool result = ExecuteCommandQueue(Batch);

            if (!result) script_abort = true; // Abort if couldn't execute first batch

            short axis1_current_enable = -1;

            short axis2_current_enable = -1;

            short axis3_current_enable = -1;

            short axis1_current_direction = -1;

            short axis2_current_direction = -1;

            short axis3_current_direction = -1;

            long axis1_current_steps = -1;

            long axis2_current_steps = -1;

            long axis3_current_steps = -1;

            long axis1_current_period = -1;

            long axis2_current_period = -1;

            long axis3_current_period = -1;

            for (int t = 0; t < treeView1.Nodes.Count && !script_abort; t++)
            {
                N = treeView1.Nodes[t];

                N.ForeColor = Color.Blue; // Change text colour of running task to blue

                N.NodeFont = B; // Change font to bold

                N.Text = N.Text; // Redraw node text

                Application.DoEvents();

                T = (task)N.Tag;

                if (T.isTimer)
                {
                    Sleep(T.mSec);
                }
                else
                {
                    Batch.Clear();

                    if (T.motor1_enabled)
                    {
                        if (axis1_current_enable != 1)
                        {
                            Batch.Enqueue("axis1 on");

                            axis1_current_enable = 1;
                        }

                        if (T.motor1_direction)
                        {
                            if (axis1_current_direction != 1)
                            {
                                Batch.Enqueue("axis1 -");
                                axis1_current_direction = 1;
                            }
                        }
                        else
                        {
                            if (axis1_current_direction != 0)
                            {
                                Batch.Enqueue("axis1 +");
                                axis1_current_direction = 0;
                            }
                        }

                        if (T.motor1_period != axis1_current_period)
                        {
                            Batch.Enqueue("axis1 period " + T.motor1_period.ToString());

                            axis1_current_period = T.motor1_period;
                        }

                        if (T.motor1_steps != axis1_current_steps)
                        {
                            Batch.Enqueue("axis1 steps " + T.motor1_steps.ToString());

                            axis1_current_steps = T.motor1_steps;
                        }
                    }
                    else
                    {
                        if (axis1_current_enable != 0)
                        {
                            Batch.Enqueue("axis1 off");

                            axis1_current_enable = 0;
                        }
                    }

                    if (T.motor2_enabled)
                    {
                        if (axis2_current_enable != 1)
                        {
                            Batch.Enqueue("axis2 on");

                            axis2_current_enable = 1;
                        }

                        if (T.motor2_direction)
                        {
                            if (axis2_current_direction != 1)
                            {
                                Batch.Enqueue("axis2 -");
                                axis2_current_direction = 1;
                            }
                        }
                        else
                        {
                            if (axis2_current_direction != 0)
                            {
                                Batch.Enqueue("axis2 +");
                                axis2_current_direction = 0;
                            }
                        }

                        if (T.motor2_period != axis2_current_period)
                        {
                            Batch.Enqueue("axis2 period " + T.motor2_period.ToString());

                            axis2_current_period = T.motor2_period;
                        }

                        if (T.motor1_steps != axis2_current_steps)
                        {
                            Batch.Enqueue("axis2 steps " + T.motor2_steps.ToString());

                            axis2_current_steps = T.motor2_steps;
                        }
                    }
                    else
                    {
                        if (axis2_current_enable != 0)
                        {
                            Batch.Enqueue("axis2 off");

                            axis2_current_enable = 0;
                        }
                    }

                    if (T.motor3_enabled)
                    {
                        if (axis3_current_enable != 1)
                        {
                            Batch.Enqueue("axis3 on");

                            axis3_current_enable = 1;
                        }

                        if (T.motor3_direction)
                        {
                            if (axis3_current_direction != 1)
                            {
                                Batch.Enqueue("axis3 -");
                                axis3_current_direction = 1;
                            }
                        }
                        else
                        {
                            if (axis3_current_direction != 0)
                            {
                                Batch.Enqueue("axis3 +");
                                axis3_current_direction = 0;
                            }
                        }

                        if (T.motor3_period != axis3_current_period)
                        {
                            Batch.Enqueue("axis3 period " + T.motor3_period.ToString());

                            axis3_current_period = T.motor3_period;
                        }

                        if (T.motor3_steps != axis3_current_steps)
                        {
                            Batch.Enqueue("axis3 steps " + T.motor3_steps.ToString());

                            axis3_current_steps = T.motor3_steps;
                        }
                    }
                    else
                    {
                        if (axis3_current_enable != 0)
                        {
                            Batch.Enqueue("axis3 off");

                            axis3_current_enable = 0;
                        }
                    }

                    Batch.Enqueue("go");

                    result = ExecuteCommandQueue(Batch); // Send axis-configuration commands, plus Go

                    if (!result) script_abort = true; // Abort script if errors occured

                    bool task_completed = false;

                    while (!task_completed && !script_abort)
                    {
                        while (InQ2.Count == 0 && !script_abort)
                        {
                            Application.DoEvents();
                        }

                        while (InQ2.Count > 0 && !script_abort)
                        {
                            string response = InQ2.Dequeue();

                            if (response == "Task completed") task_completed = true;
                        }
                    }

                }

                N.ForeColor = Color.Black; // Restore text colour of node

                N.NodeFont = Old;

            }

            Batch.Enqueue("screen 1");

            ExecuteCommandQueue(Batch);

            script_running = false;

            UpdateControls();

        }

        private void MoveNode(bool direction_down)
        {
            // This function swaps a node with either its previous or its next node

            TreeNode T1 = treeView1.SelectedNode;

            if (T1 == null) return;

            TreeNode T2;

            if (direction_down)
                T2 = T1.NextNode;
            else
                T2 = T1.PrevNode;


            if (T2 == null) return;

            int index1 = T1.Index;

            int index2 = T2.Index;

            treeView1.Nodes.Remove(T1);

            treeView1.Nodes.Remove(T2);

            string temp = T1.Text;

            T1.Text = T2.Text;

            T2.Text = temp;

            if (direction_down)
            {
                treeView1.Nodes.Insert(index1, T1);

                treeView1.Nodes.Insert(index1, T2);
            }
            else
            {

                treeView1.Nodes.Insert(index2, T1);

                treeView1.Nodes.Insert(index1, T2);
            }

            treeView1.SelectedNode = T1;
        }

        private void New()
        {
            // This function handles the clicking of the "New" toolbar button or menu item
            treeView1.Nodes.Clear();

            OpenedFile = "";

            UpdateControls();
        }

        private void Sleep(long mSec)
        {

            // This function creates an application delay

            int start = Environment.TickCount;

            while (Environment.TickCount - start < mSec && !script_abort) Application.DoEvents();

        }

        private void Open()
        {
            // This function handles the clicking of the "Open" toolbar button or menu item

            openFileDialog1.FileName = "";

            openFileDialog1.ShowDialog();

            try
            {

                if (openFileDialog1.FileName != "")
                    LoadScriptFromFile(openFileDialog1.FileName);

                OpenedFile = openFileDialog1.FileName;

                UpdateControls();
            }
            catch (Exception)
            {
                MessageBox.Show("The specified file could not be loaded!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void SaveAs()
        {
            // This function handles the clicking of the "Save As" toolbar button or menu item

            saveFileDialog1.FileName = "";

            saveFileDialog1.ShowDialog(); // Display SaveAs dialog window

            if (saveFileDialog1.FileName != "")
            {

                SaveScriptToFile(saveFileDialog1.FileName);

                OpenedFile = saveFileDialog1.FileName;

            }

            UpdateControls();

        }

        private void Save()
        {
            // This function handles the clicking of the "Save" toolbar button or menu item

            if (OpenedFile == "")
            {
                SaveAs();
            }
            else
            {
                SaveScriptToFile(OpenedFile);
            }

            UpdateControls();
        }

        private void UpdateControls()
        {
            // This function updates the enabled/disabled status for all the application buttons and menu items

            // It is called whenever any change to any of the determining factors occurs,

            // (e.g. connection staus change, load script, ...etc)

            TreeNode S = treeView1.SelectedNode;

            bool IsNodeSelected = (S != null);

            bool isRoot = false;

            bool isFirst = false;

            bool isLast = false;

            bool isFileLoaded = (OpenedFile != "");

            if (IsNodeSelected) isRoot = (S.Level == 0);

            if (IsNodeSelected) isFirst = (S.PrevNode == null);

            if (IsNodeSelected) isLast = (S.NextNode == null);

            toolStripButton5.Enabled = isRoot && IsNodeSelected && !script_running; // Delete Task Button

            toolStripButton6.Enabled = isRoot && !isFirst && !script_running; // Move Up Button

            toolStripButton3.Enabled = isRoot && !isLast && !script_running; // Move Down Button

            this.Text = (isFileLoaded ? ExtractFileName(OpenedFile) + " - " : "") + "Stepper Motor Controller"; // Main window title

            saveToolStripButton.Enabled = (treeView1.Nodes.Count != 0); // Disable Save menu item

            saveScriptToolStripMenuItem.Enabled = (treeView1.Nodes.Count != 0); // Disable Save button

            toolStripMenuItem2.Enabled = (treeView1.Nodes.Count != 0); // Disable Save As menu item

            button1.Enabled = !port_busy && !script_running && serialPort1.IsOpen ; // Send Button

            runScriptToolStripMenuItem.Text = (script_running ? "&Stop Script" : "&Run Script");

            runScriptToolStripMenuItem.ShortcutKeys = (script_running ? Keys.F6 : Keys.F5);

            runScriptToolStripMenuItem.Enabled = (treeView1.Nodes.Count != 0 && serialPort1.IsOpen);

            toolStripButton2.Enabled = !script_running; // New File toolbar button

            openToolStripButton.Enabled = !script_running; // Open File toolbar button

            NewToolStripMenuItem.Enabled = !script_running; // New File menu item

            loadScriptToolStripMenuItem.Enabled = !script_running; // Open File menu item

            toolStripButton10.Enabled = serialPort1.IsOpen && treeView1.Nodes.Count > 0 && !script_running; // Run Script Button

            toolStripButton11.Enabled = serialPort1.IsOpen && treeView1.Nodes.Count > 0 && script_running; // Stop Script Button

            testHardwareToolStripMenuItem.Enabled = serialPort1.IsOpen && !script_running; // Test Hardware Menu item

            toolStripButton4.Enabled = !script_running; // New Task Button

            toolStripButton1.Enabled = !script_running; // New Timer Task Button

            connectToolStripMenuItem1.Enabled = !script_running; // Connect menu item

            connectToolStripMenuItem1.Text = (!serialPort1.IsOpen) ? "Connect" : "Disconnect";

            toolStripStatusLabel1.Text = (serialPort1.IsOpen) ? "Connected to " + serialPort1.PortName + " @ " + serialPort1.BaudRate.ToString() + " baud" : "Not Connected";

            toolStripStatusLabel1.Image = (!serialPort1.IsOpen) ? Properties.Resources.lightbulb_off : Properties.Resources.lightbulb;

            connectToolStripMenuItem1.Image = (serialPort1.IsOpen) ? Properties.Resources.disconnect : Properties.Resources.connect;

        }

        private void LoadScriptFromFile(string FilePath)
        {
            // This function loads a comma-delimited string from a script file, parses it,

            // then recreates the original script

            string SC = File.ReadAllText(FilePath); // Script Content

            treeView1.Nodes.Clear();

            string[] A = SC.Split(',');

            treeView1.BeginUpdate();

            for (int t = 0; t < A.Length - 1; t += 15)
            {
                task T = new task();

                T.motor1_enabled = Convert.ToBoolean(A[t + 1]);
                T.motor2_enabled = Convert.ToBoolean(A[t + 2]);
                T.motor3_enabled = Convert.ToBoolean(A[t + 3]);

                T.motor1_direction = Convert.ToBoolean(A[t + 4]);
                T.motor2_direction = Convert.ToBoolean(A[t + 5]);
                T.motor3_direction = Convert.ToBoolean(A[t + 6]);

                T.motor1_period = (long)Convert.ToInt64(A[t + 7]);
                T.motor2_period = (long)Convert.ToInt64(A[t + 8]);
                T.motor3_period = (long)Convert.ToInt64(A[t + 9]);

                T.motor1_steps = Convert.ToInt64(A[t + 10]);
                T.motor2_steps = Convert.ToInt64(A[t + 11]);
                T.motor3_steps = Convert.ToInt64(A[t + 12]);

                T.isTimer = Convert.ToBoolean(A[t + 13]);
                T.mSec = (long)Convert.ToInt64(A[t + 14]);

                AddTaskToTree(T);
            }

            treeView1.EndUpdate();




        }

        private void SaveScriptToFile(string FilePath)
        {
            // This function converts the current script into a comma-delimited string containing

            // properties of all nodes in order, then saves the string to a script file in plain text

            string SC = ""; // Script Content

            string TC = ""; // Task Content

            string d = ","; // Delimiter

            TreeNode N;

            task T;

            for (int t = 0; t < treeView1.Nodes.Count; t++)
            {
                N = treeView1.Nodes[t];

                if (N.Level == 0)
                {
                    T = (task)N.Tag;

                    TC = N.Text + d;

                    TC += T.motor1_enabled + d;
                    TC += T.motor2_enabled + d;
                    TC += T.motor3_enabled + d;

                    TC += T.motor1_direction + d;
                    TC += T.motor2_direction + d;
                    TC += T.motor3_direction + d;

                    TC += T.motor1_period + d;
                    TC += T.motor2_period + d;
                    TC += T.motor3_period + d;

                    TC += T.motor1_steps + d;
                    TC += T.motor2_steps + d;
                    TC += T.motor3_steps + d;

                    TC += T.isTimer + d;
                    TC += T.mSec + d;

                    SC += TC;
                }

            }

            File.WriteAllText(FilePath, SC);
        }

        private void NodeEdit()
        {
            // This function handles the editing of property nodes

            // It is called whenever the tree view control is double-clicked

            TreeNode S = treeView1.SelectedNode;

            if (S == null) return; // If no node selected, exit

            if (S.Level == 0) return; // If node is root, exit

            TreeNode MotorNode = null;

            TreeNode TaskNode = null;

            if (S.Level == 2)
            {
                MotorNode = S.Parent;

                TaskNode = MotorNode.Parent;
            }
            else
            {
                TaskNode = S.Parent;
            }

            task T = (task)TaskNode.Tag;

            if (S.Name == "P1")
            {
                // Enabled Property Node

                if (MotorNode.Name == "M1") T.motor1_enabled = !T.motor1_enabled;
                if (MotorNode.Name == "M2") T.motor2_enabled = !T.motor2_enabled;
                if (MotorNode.Name == "M3") T.motor3_enabled = !T.motor3_enabled;

            }

            if (S.Name == "P2")
            {
                // Direction Property Node

                if (MotorNode.Name == "M1") T.motor1_direction = !T.motor1_direction;
                if (MotorNode.Name == "M2") T.motor2_direction = !T.motor2_direction;
                if (MotorNode.Name == "M3") T.motor3_direction = !T.motor3_direction;

            }

            if (S.Name == "P3")
            {
                // Steps Property Node

                Form2 F = new Form2();

                if (MotorNode.Name == "M1") F.prev = T.motor1_steps;
                if (MotorNode.Name == "M2") F.prev = T.motor2_steps;
                if (MotorNode.Name == "M3") F.prev = T.motor3_steps;

                if (MotorNode.Name == "M1") F.Text = "Steps for Stepper Motor 1";
                if (MotorNode.Name == "M2") F.Text = "Steps for Stepper Motor 2";
                if (MotorNode.Name == "M3") F.Text = "Steps for Stepper Motor 3";

                F.ShowDialog();

                if (MotorNode.Name == "M1") T.motor1_steps = F.result;
                if (MotorNode.Name == "M2") T.motor2_steps = F.result;
                if (MotorNode.Name == "M3") T.motor3_steps = F.result;

            }

            if (S.Name == "P4")
            {
                // Steps Period Node

                Form3 F = new Form3();

                if (MotorNode.Name == "M1") F.prev = T.motor1_period;
                if (MotorNode.Name == "M2") F.prev = T.motor2_period;
                if (MotorNode.Name == "M3") F.prev = T.motor3_period;

                if (MotorNode.Name == "M1") F.Text = "Period for Stepper Motor 1";
                if (MotorNode.Name == "M2") F.Text = "Period for Stepper Motor 2";
                if (MotorNode.Name == "M3") F.Text = "Period for Stepper Motor 3";

                F.ShowDialog();

                if (MotorNode.Name == "M1") T.motor1_period = F.result;
                if (MotorNode.Name == "M2") T.motor2_period = F.result;
                if (MotorNode.Name == "M3") T.motor3_period = F.result;

            }

            if (S.Name == "Timer")
            {
                // Timer Node
                Form4 F = new Form4();

                F.prev = T.mSec;

                F.ShowDialog();

                T.mSec = F.result;
            }

            TreeNode N;

            for (int x = 0; x < treeView1.Nodes.Count; x++)
            {

                N = treeView1.Nodes[x];

                if (N.Text == TaskNode.Text) ModifyTreeNode(N, T);

            }



        }

        private TreeNode GenerateTreeNode(task T)
        {
            // This function creates a tree node object and populate it with the details of task T

            TreeNode N = new TreeNode();

            N.Tag = T;

            if (T.isTimer)
            {
                TreeNode Timer = N.Nodes.Add("Timer", "Delay: " + T.mSec.ToString() + " ms");
                Timer.ImageIndex = 3;
                Timer.SelectedImageIndex = 3;
            }
            else
            {

                TreeNode M1 = N.Nodes.Add("M1", "Stepper Motor 1", 1, 1);
                TreeNode M2 = N.Nodes.Add("M2", "Stepper Motor 2", 1, 1);
                TreeNode M3 = N.Nodes.Add("M3", "Stepper Motor 3", 1, 1);

                TreeNode M1P1 = M1.Nodes.Add("P1", "", 2, 2);
                TreeNode M1P2 = M1.Nodes.Add("P2", "", 2, 2);
                TreeNode M1P3 = M1.Nodes.Add("P3", "", 2, 2);
                TreeNode M1P4 = M1.Nodes.Add("P4", "", 2, 2);

                TreeNode M2P1 = M2.Nodes.Add("P1", "", 2, 2);
                TreeNode M2P2 = M2.Nodes.Add("P2", "", 2, 2);
                TreeNode M2P3 = M2.Nodes.Add("P3", "", 2, 2);
                TreeNode M2P4 = M2.Nodes.Add("P4", "", 2, 2);

                TreeNode M3P1 = M3.Nodes.Add("P1", "", 2, 2);
                TreeNode M3P2 = M3.Nodes.Add("P2", "", 2, 2);
                TreeNode M3P3 = M3.Nodes.Add("P3", "", 2, 2);
                TreeNode M3P4 = M3.Nodes.Add("P4", "", 2, 2);

                M1P1.Text = CreateEnLabel(T.motor1_enabled);
                M1P2.Text = CreateDirLabel(T.motor1_direction);
                M1P3.Text = CreateStepsLabel(T.motor1_steps);
                M1P4.Text = CreatePFLabel(T.motor1_period);

                M2P1.Text = CreateEnLabel(T.motor2_enabled);
                M2P2.Text = CreateDirLabel(T.motor2_direction);
                M2P3.Text = CreateStepsLabel(T.motor2_steps);
                M2P4.Text = CreatePFLabel(T.motor2_period);

                M3P1.Text = CreateEnLabel(T.motor3_enabled);
                M3P2.Text = CreateDirLabel(T.motor3_direction);
                M3P3.Text = CreateStepsLabel(T.motor3_steps);
                M3P4.Text = CreatePFLabel(T.motor3_period);
            }
            return N;

        }

        private void ModifyTreeNode(TreeNode N, task T)
        {
            // This function updates an existing tree node with the details of a task T

            if (T.isTimer)
            {
                N.Nodes[0].Text = "Timer: " + T.mSec.ToString() + " ms";

            }
            else
            {
                TreeNode M1P1 = N.Nodes[0].Nodes[0];
                TreeNode M1P2 = N.Nodes[0].Nodes[1];
                TreeNode M1P3 = N.Nodes[0].Nodes[2];
                TreeNode M1P4 = N.Nodes[0].Nodes[3];

                TreeNode M2P1 = N.Nodes[1].Nodes[0];
                TreeNode M2P2 = N.Nodes[1].Nodes[1];
                TreeNode M2P3 = N.Nodes[1].Nodes[2];
                TreeNode M2P4 = N.Nodes[1].Nodes[3];

                TreeNode M3P1 = N.Nodes[2].Nodes[0];
                TreeNode M3P2 = N.Nodes[2].Nodes[1];
                TreeNode M3P3 = N.Nodes[2].Nodes[2];
                TreeNode M3P4 = N.Nodes[2].Nodes[3];

                M1P1.Text = CreateEnLabel(T.motor1_enabled);
                M1P2.Text = CreateDirLabel(T.motor1_direction);
                M1P3.Text = CreateStepsLabel(T.motor1_steps);
                M1P4.Text = CreatePFLabel(T.motor1_period);

                M2P1.Text = CreateEnLabel(T.motor2_enabled);
                M2P2.Text = CreateDirLabel(T.motor2_direction);
                M2P3.Text = CreateStepsLabel(T.motor2_steps);
                M2P4.Text = CreatePFLabel(T.motor2_period);

                M3P1.Text = CreateEnLabel(T.motor3_enabled);
                M3P2.Text = CreateDirLabel(T.motor3_direction);
                M3P3.Text = CreateStepsLabel(T.motor3_steps);
                M3P4.Text = CreatePFLabel(T.motor3_period);
            }

        }

        private string ExtractFileName(string FilePath)
        {
            // This function extracts the file name (without the extension) of a script file path

            // e.g. converts "C:\\folder\\myscript.csc" to "myscript"

            int i = FilePath.LastIndexOf("\\");

            return FilePath.Remove(0, i + 1).Replace(".csc", "");
        }

        private void AddTaskToTree(task T)
        {
            // This function creates a tree node, populate it with the details of task T

            // then add it to the tree view

            int index = treeView1.Nodes.Count + 1;

            TreeNode A = GenerateTreeNode(T);

            A.Text = "Task " + index.ToString();

            treeView1.Nodes.Add(A);

        }

        private string CreateEnLabel(bool e)
        {
            // Create Enabled Label

            // This function creates the text for an Enabled node

            if (e)
                return "Enabled: Yes";
            else
                return "Enabled: No";
        }

        private string CreateDirLabel(bool d)
        {
            // Create Direction Label

            // This function creates the text for a Direction node

            if (d)
                return "Direction: Backward";
            else
                return "Direction: Forward";

        }

        private string CreateStepsLabel(long s)
        {
            // Create Steps Label

            // This function creates the text for a Steps node

            if (s > 0)

                return "Steps: " + s.ToString();
            else
                return "Steps: Infinite";
        }

        private string GeneratePLabel(long mSec)
        {
            // Create Period Label

            // This function creates the text for a timer Delay node

            string result = "";

            if (mSec >= 1e3)
            {
                long Sec = mSec / 1000;

                result = "Period: " + Sec.ToString() + " s";

            }
            else
            {
                result = "Period: " + mSec.ToString() + " ms";

            }
            return result;
        }

        private string CreatePFLabel(long mSec)
        {
            // Create Period-Frequency Label

            // The function create the text of a Period-Frequency node

            double frequency = 1 / (mSec * 1e-3);

            string result = "";

            if (mSec >= 1e3)
            {
                long Sec = mSec / 1000;

                result = "Period: " + Sec.ToString() + " s";

                result += " , Frequency: " + frequency.ToString("0.##") + " Hz";
            }

            else
            {
                result = "Period: " + mSec.ToString() + " ms";

                result += " , Frequency: " + frequency.ToString("0.##") + " Hz";
            }



            return result;
        }

    }
}