using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace StepperMotorController
{
    public partial class Form4 : Form
    {
        public long result = 0;

        public long prev = 0;

        public Form4()
        {
            InitializeComponent();
        }

        private void Form4_Load(object sender, EventArgs e)
        {

            textBox1.Text = prev.ToString();

            textBox1.SelectAll();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (ValidateInput()) this.Close();
        }
        private bool ValidateInput()
        {
            try
            {
                result = (long)Convert.ToInt32(textBox1.Text);

                if (result <= 0) throw (new Exception());

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            result = prev;
        }

    }
}