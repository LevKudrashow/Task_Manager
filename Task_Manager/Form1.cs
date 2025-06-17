using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Task_Manager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnManageWorkers_Click(object sender, System.EventArgs e)
        {
            WorkersForm workersForm = new WorkersForm();
            workersForm.Show();
        }

        private void btnManageTasks_Click(object sender, System.EventArgs e)
        {
            TasksForm tasksForm = new TasksForm();
            tasksForm.Show();
        }

        private void btnAssignTasks_Click(object sender, System.EventArgs e)
        {
            AssignTaskForm assignTaskForm = new AssignTaskForm();
            assignTaskForm.Show();
        }
    }
}
