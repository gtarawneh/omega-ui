using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace StepperMotorController
{
    

    public partial class Form2 : Form
    {
        public long result = 0;

        public long prev = 0;

        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            if (prev!=0)
            {
                textBox1.Text = prev.ToString();

                textBox1.SelectAll();
            }
            else
            {
                checkBox1.Checked = true;

                textBox1.Enabled = false;
            }

            result = prev;

        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (ValidateInput()) this.Close();

        }

        private bool ValidateInput()
        {
            try
            {
                if (checkBox1.Checked)
                {
                    result = 0;
                }
                else
                {
                    result = (long)Convert.ToInt32(textBox1.Text);

                    if (result <= 0) throw (new Exception());
                }
                
                return true;

            }
            catch (Exception)
            {
                MessageBox.Show(this, "Invalid Numerical Value! Please enter an integer number between 1 and 4294967296.", "Error");

                textBox1.SelectAll();

                return false ;
            }

            

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox1.Enabled = !checkBox1.Checked;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            result = prev;
        }
    }
}