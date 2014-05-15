using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace StepperMotorController
{
    public partial class Form6 : Form
    {
        public Form6()
        {
            
        }

        public Form6(string title, string message1, string message2, bool good)
        {
            InitializeComponent();

            pictureBox2.Visible = good;
            pictureBox1.Visible = !good;

            label2.Text = message1;

            label3.Text = message2;

            this.Text = title;

            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form6_Load(object sender, EventArgs e)
        {
            

        }
    }
}