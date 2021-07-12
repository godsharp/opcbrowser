using System;
using System.Windows.Forms;

namespace OpcDaBrowser
{
    public partial class FormValueInput : Form
    {
        private readonly Action<string> _callback;

        public FormValueInput(Action<string> callback)
        {
            _callback = callback;
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Write();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Write();
            Close();
        }

        private void Write()
        {
            _callback(textBox1.Text);
        }
    }
}
