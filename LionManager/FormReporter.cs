using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LionLibrary;

namespace Lion
{
    public partial class FormReporter : Form
    {
        private readonly LionReporter _reporter;
        public FormReporter(LionReporter reporter)
        {
            _reporter = reporter;

            InitializeComponent();
        }

        private void FormReporter_Load(object sender, EventArgs e)
        {
            textLog.Text = string.Join(Environment.NewLine,
                _reporter.Entries.Select(each => each.ToString()));
            textLog.Select(0, 0);
        }
    }
}
