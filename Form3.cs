using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace StepperMotorController
{
    public partial class Form3 : Form
    {
        public long result = 0;

        public long prev = 0;

        public Form3()
        {
            InitializeComponent();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            if (prev != 0)
            {
                textBox1.Text = prev.ToString();

                textBox1.SelectAll();
            }
            else
            {
                textBox1.Enabled = false;
            }

            result = prev;

            UpdateFrequency();
        }

        private void UpdateFrequency()
        {
            double p = Convert.ToInt32(textBox1.Text) * 1e-3;

            double f = 1 / p;

            textBox2.Text = f.ToString("0.##");
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

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (ValidateInput())
            {
                UpdateFrequency();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (ValidateInput())
            {
                this.Close();
            }
            else
            {
                MessageBox.Show(this, "Invalid Numerical Value! Please enter an integer number between 1 and 2147483647.", "Error");
                
                textBox1.SelectAll();
            }


        }
    }
}