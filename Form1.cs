using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Data;
using System.Data.SQLite;
using System.Collections;
using System.Xml;

namespace GRASPOfflineProcessor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void message(String txt)
        {
            textBox1.Text = txt + "\r\n" + textBox1.Text;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            message("Choose a GRASP directory and then click on \"Start\".");

        }

        private bool isAGRASPDir()
        {
            bool validStructure = true;

            // Check conditions
            if(!Directory.Exists(folderBrowserDialog1.SelectedPath+"\\database")) {
                validStructure &= false;
                if (!File.Exists(folderBrowserDialog1.SelectedPath + "\\database\\message.db"))
                {
                    validStructure &= false;
                }
                if (!File.Exists(folderBrowserDialog1.SelectedPath + "\\database\\forms.db"))
                {
                    validStructure &= false;
                }
            }
            if (!Directory.Exists(folderBrowserDialog1.SelectedPath + "\\forms"))
            {
                validStructure &= false;
            }
            if (!Directory.Exists(folderBrowserDialog1.SelectedPath + "\\instances"))
            {
                validStructure &= false;
            }
            if (!Directory.Exists(folderBrowserDialog1.SelectedPath + "\\metadata"))
            {
                validStructure &= false;
            }
            if (Directory.Exists(folderBrowserDialog1.SelectedPath + "\\metadata"))
            {
                if (!File.Exists(folderBrowserDialog1.SelectedPath + "\\metadata\\message.db"))
                {
                    validStructure &= false;
                }
                if (!File.Exists(folderBrowserDialog1.SelectedPath + "\\metadata\\forms.db"))
                {
                    validStructure &= false;
                }
            }
            return validStructure;
        }

        private void processForms()
        {
            SQLiteCommand command;
            SQLiteDataReader reader;
            String str;
            string[] path;
            XmlDocument doc = new XmlDocument();
            if (!Directory.Exists(folderBrowserDialog1.SelectedPath + "\\output\\"))
            {
                Directory.CreateDirectory(folderBrowserDialog1.SelectedPath + "\\output\\");
            }
            FileStream f;
            StreamWriter sw;
            
            XmlDocument spec = new XmlDocument();
            XmlNodeList spec_data;

            XmlNodeList valuenode;

            List<string> rosters = new List<string>();
            foreach (string data in currentForms)
            {

                f = File.Create(folderBrowserDialog1.SelectedPath + "\\output\\" + textBox2.Text + data + ".csv");
                sw = new StreamWriter(f);

                command = new SQLiteCommand("SELECT instanceFilePath FROM forms where displayName='" +
                    data + "' AND (status='completed' OR status='submitted')",conn);
                reader = command.ExecuteReader();


                // LOAD File Header:
                spec.Load(folderBrowserDialog1.SelectedPath + "\\forms\\" + data + ".xml");

                spec_data = spec.GetElementsByTagName("data");
                

                sw.Write("formId,");
                foreach (XmlNode datanode in spec_data[0].ChildNodes)
                {
                    if (datanode.ChildNodes.Count == 0)
                    {
                        sw.Write(datanode.Name + ",");
                    }
                    else if (datanode.ChildNodes.Count > 1) // ROSTER
                    {
                        // Create datafile for roaster:
                        FileStream roster_file = File.Create(folderBrowserDialog1.SelectedPath + "\\output\\" + textBox2.Text + data + "_" + datanode.Name + ".csv");
                        StreamWriter r_writer = new StreamWriter(roster_file);
                        r_writer.Write("formId,");
                        foreach (XmlNode subNode in datanode.ChildNodes)
                        {
                            r_writer.Write(subNode.Name+",");
                        }
                        r_writer.Write("\r\n");
                        r_writer.Close();
                        roster_file.Close();
                    }
                }
                sw.Write("\r\n");

                while (reader.Read())
                {
                    str = reader["instanceFilePath"].ToString();
                    path = str.Split("/".ToCharArray());

                    doc.Load(folderBrowserDialog1.SelectedPath + "\\instances\\" + path[path.Length - 2] + "\\" + path[path.Length - 1]);


                    sw.Write(textBox2.Text + path[path.Length - 1] + ",");
                    foreach (XmlNode datanode in spec_data[0].ChildNodes)
                    {
                        if (datanode.ChildNodes.Count == 0)
                        {
                            valuenode = doc.GetElementsByTagName(datanode.Name);
                            if (valuenode.Count > 0)
                            {
                                sw.Write(valuenode[0].InnerText.Replace(',', ' ').Replace('\r', ' ').Replace('\n', ' ') + ",");
                            }
                            else
                            {
                                sw.Write("empty,");
                            }
                        }
                        else if (datanode.ChildNodes.Count > 1) // PROCESS ROSTER
                        {
                            // Create datafile for roaster:
                            FileStream roster_file = File.Open(folderBrowserDialog1.SelectedPath + "\\output\\" + textBox2.Text + data + "_" + datanode.Name + ".csv", FileMode.Append, FileAccess.Write);
                            StreamWriter r_writer = new StreamWriter(roster_file);
                            

                            XmlNodeList rosterData = doc.GetElementsByTagName(datanode.Name);

                            foreach (XmlNode node in rosterData)
                            {
                                XmlDocument newDoc = new XmlDocument();

                                // Treat each field as a new XML Document
                                // TO DO: Change to XPath or something else.
                                newDoc.LoadXml("<"+node.Name+">"+node.InnerXml+"</"+node.Name+">");

                                r_writer.Write(textBox2.Text + path[path.Length - 1] + ",");
                                foreach (XmlNode subNode in datanode.ChildNodes)
                                {
                                    valuenode = newDoc.GetElementsByTagName(subNode.Name);
                                    if (valuenode.Count > 0)
                                    {
                                        r_writer.Write(valuenode[0].InnerText.Replace(',', ' ').Replace('\r', ' ').Replace('\n', ' ') + ",");
                                    }
                                    else
                                    {
                                        r_writer.Write("empty,");
                                    }
                                }
                                r_writer.Write("\r\n");
                            }
                            r_writer.Flush();
                            roster_file.Close();
                        }
                    }
                    sw.Write("\r\n");
                    message(path[path.Length - 1] + " written.");
                }


                sw.Flush();
                reader.Close();
                f.Close();
            }
            conn.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                if (isAGRASPDir())
                {
                    message("Notice: GRASP Directory detected.\r\nCurrent working directory: " + folderBrowserDialog1.SelectedPath);
                    message("Click \"start\" to process the questionnaires.");
                    button2.Enabled = true;
                }
                else
                {
                    message("Error: The selected directory isn't a valid GRASP directory.");
                    button2.Enabled = false;
                }
            }
            else
            {
                message("Notice: You must select a directory.");
            }
        }

        SQLiteConnection conn = null;
        List<string> currentForms;
        
        private void button2_Click(object sender, EventArgs e)
        {
            currentForms = new List<string>();

            conn = new SQLiteConnection("Data Source="+folderBrowserDialog1.SelectedPath + "\\metadata\\forms.db");

            conn.Open();

            SQLiteCommand listContents = new SQLiteCommand(@"
SELECT status,count(*) as total FROM forms GROUP BY status
            ", conn);

            SQLiteDataReader reader = listContents.ExecuteReader();

            string count_msg = "";
            if (reader.HasRows)
            {
                count_msg += "Questionnaires found:\r\n";
            }
            else
            {
                count_msg += "Error: Metadata is empty, cannot continue.";
                button2.Enabled = false;
                return;
            }
            while (reader.Read())
            {
                count_msg += reader["status"].ToString() + ":" + reader["total"] + "\r\n";
            }
            message(count_msg);

            reader.Close();

            SQLiteCommand listForms = new SQLiteCommand(@"
SELECT displayName FROM forms where status='new'
            ", conn);

            reader = listForms.ExecuteReader();
            string forms_msg = "Forms found: \r\n";
            while (reader.Read())
            {
               forms_msg += reader["displayName"].ToString()+"\r\n";
               currentForms.Add(reader["displayName"].ToString()); // Adds forms to process.
            }
            message(forms_msg);

            processForms();



            button2.Enabled = false;
        }

    }
}
