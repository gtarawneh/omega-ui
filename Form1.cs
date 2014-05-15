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

        public Form1()
        {
            InitializeComponent();

            splitContainer1.Panel2Collapsed = true;

            Initialize();

            UpdateControls();
        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            New();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            task T = new task();

            AddTaskToTree(T);

            UpdateControls();

        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            // Delete Task

            TreeNode S = treeView1.SelectedNode;

            if (S.Level != 0) return; // If not root node, exit

            treeView1.Nodes.Remove(S); // Delete node

            // Now re-arranging task numbers

            int index = 1;

            for (int t = 0; t < treeView1.Nodes.Count; t++)
            {
                if (treeView1.Nodes[t].Level == 0)
                {
                    treeView1.Nodes[t].Text = "Task " + index.ToString();

                    index++;
                }
            }


            UpdateControls();
        }

        private void treeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (!script_running) NodeEdit();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            UpdateControls();
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            Open();
        }

        private void saveScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void loadScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Open();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            New();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            task T = new task();

            T.isTimer = true;

            T.mSec = 1000;

            AddTaskToTree(T);

            UpdateControls();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            MoveNode(true);

            UpdateControls();

        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            MoveNode(false);

            UpdateControls();
        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            textBox1.Focus();

            Application.DoEvents();

            RunScript();

        }

        private void testHardwareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            script_abort = false;

            Queue<string> Batch = new Queue<string>();

            Batch.Enqueue("ping");

            InQ2.Clear(); // Clear processing Q

            if (ExecuteCommandQueue(Batch))

                (new Form6("Hardware Test", "The controller is up and running!", "", true)).ShowDialog();

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen) serialPort1.Close();
            }
            catch (Exception)
            { }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\n' || e.KeyChar == '\r')
            {
                if (!port_busy && !script_running)
                {
                    button1_Click(null, null);
                }

                    e.Handled = true;
                
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SendLineSlow(textBox2.Text);

            textBox2.Text = "";
        }

        private void consoleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (consoleToolStripMenuItem.Checked)
            {
                splitContainer1.Panel2Collapsed = true;

                consoleToolStripMenuItem.Checked = false;
            }
            else
            {
                splitContainer1.Panel2Collapsed = false;

                consoleToolStripMenuItem.Checked = true;

                textBox2.Focus();
            }
        }

        private void connectToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
            }
            else
            {
                try
                {
                    serialPort1.Open();
                }
                catch (Exception)
                {
                    (new Form6("Error", "Unable to open COM1!", "Make sure that no other application is using the port.", false)).ShowDialog ();
                    
                }


            }

            UpdateControls();
        }

        private void clearConsoleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
        }

        private void copyConsoleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox1.Text);
        }

        private void expandTasksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode N;

            treeView1.BeginUpdate();

            for (int t = 0; t < treeView1.Nodes.Count; t++)
            {
                N = treeView1.Nodes[t];

                N.Collapse();


            }

            treeView1.EndUpdate();

        }

        private void expandTasksMotorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode N;

            treeView1.BeginUpdate();

            for (int t = 0; t < treeView1.Nodes.Count; t++)
            {
                N = treeView1.Nodes[t];

                N.Expand();

                foreach (TreeNode C in N.Nodes) C.Collapse();

            }

            treeView1.EndUpdate();
        }

        private void exToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode N;

            treeView1.BeginUpdate();

            for (int t = 0; t < treeView1.Nodes.Count; t++)
            {
                N = treeView1.Nodes[t];

                N.ExpandAll();

            }

            treeView1.EndUpdate();
        }

        private void NewToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            New();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form5 F = new Form5();

            F.ShowDialog();

        }

        private void toolStripButton9_Click(object sender, EventArgs e)
        {
            aboutToolStripMenuItem_Click(null, null);
        }

        private void runScriptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (script_running)
                toolStripButton11_Click(null, null);
            else
                toolStripButton10_Click(null, null);
        }

        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            script_abort = true;
            SendLineSlow("stop");
        }

        private void testCommunicationSpeedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double  result = TestSpeed();

            MessageBox.Show("Average: " + result.ToString() + " ms per instruction");
        }

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            SerialDataReceived();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Queue<double> T = new Queue<double>();

            for (double t = 0; t < 5000; t++)
            {
                T.Enqueue(t);
            }

            MessageBox.Show("done");
        }

       

    }
}