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
    public partial class FormSchemaEditor : Form
    {
        private readonly StringXmlSchema _schema;
        public FormSchemaEditor(StringXmlSchema schema)
        {
            _schema = schema;
            DialogResult = DialogResult.Cancel;

            InitializeComponent();
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            foreach (var item in checkedAttributes.CheckedItems.OfType<SchemaItem>())
            {
                item.SchemaNode.Attributes[item.AttributeName] = StringXmlSchema.AttributeType.String;
            }

            DialogResult = DialogResult.OK;
        }

        private void FormSchemaEditor_Load(object sender, EventArgs e)
        {
            _schema.Enumerate(node =>
            {
                var xpath = node.XPath;
                foreach (var attributeName in node.Attributes.Keys)
                {
                    checkedAttributes.Items.Add(new SchemaItem
                    {
                        SchemaNode = node, XPath = xpath, AttributeName = attributeName
                    });
                }
            });
        }

        private struct SchemaItem
        {
            public StringXmlSchema.Node SchemaNode;
            public string XPath;
            public string AttributeName;

            public override string ToString()
            {
                return XPath + "/@" + AttributeName;
            }
        }
    }
}
