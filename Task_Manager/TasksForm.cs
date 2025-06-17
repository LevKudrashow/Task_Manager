using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql;

namespace Task_Manager
{
    public partial class TasksForm : Form
    {
        public TasksForm()
        {
            InitializeComponent();
            LoadTasks();
        }

        private void LoadTasks()
        {
            using (var conn = DBHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                SELECT 
                    t.id, 
                    t.title,
                    t.description,
                    t.is_busy
                FROM tasks t";

                var cmd = new NpgsqlCommand(query, conn);
                var adapter = new NpgsqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dataGridView.DataSource = dt;
            }
        }

        private void BtnAddTask_Click(object sender, EventArgs e)
        {
            AddTaskForm addForm = new AddTaskForm();
            addForm.FormClosed += (s, args) => LoadTasks();
            addForm.Show();
        }

        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (e.ColumnIndex == dataGridView.Columns["btnEdit"].Index)
            {
                int id = Convert.ToInt32(dataGridView.Rows[e.RowIndex].Cells["id"].Value);
                EditTaskForm editForm = new EditTaskForm(id);
                editForm.FormClosed += (s, args) => LoadTasks();
                editForm.Show();
            }
            else if (e.ColumnIndex == dataGridView.Columns["btnDelete"].Index)
            {
                int id = Convert.ToInt32(dataGridView.Rows[e.RowIndex].Cells["id"].Value);
                if (MessageBox.Show("Удалить задачу?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    DeleteTask(id);
                    LoadTasks();
                }
            }
        }

        private void DeleteTask(int taskId)
        {
            using (var conn = DBHelper.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Проверяем, занята ли задача
                        bool isBusy = false;
                        int? workerId = null;

                        var checkCmd = new NpgsqlCommand(
                            "SELECT is_busy, (SELECT worker_id FROM assignments WHERE task_id = @id) FROM tasks WHERE id = @id",
                            conn);
                        checkCmd.Parameters.AddWithValue("@id", taskId);

                        using (var reader = checkCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                isBusy = reader.GetBoolean(0);
                                if (!reader.IsDBNull(1))
                                    workerId = reader.GetInt32(1);
                            }
                        }

                        // 2. Если задача занята - освобождаем работника
                        if (isBusy && workerId.HasValue)
                        {
                            // Освобождаем работника
                            var freeWorkerCmd = new NpgsqlCommand(
                                "UPDATE workers SET is_busy = false WHERE id = @workerId",
                                conn);
                            freeWorkerCmd.Parameters.AddWithValue("@workerId", workerId.Value);
                            freeWorkerCmd.ExecuteNonQuery();

                            // Удаляем связь
                            var deleteAssignmentCmd = new NpgsqlCommand(
                                "DELETE FROM assignments WHERE task_id = @taskId AND worker_id = @workerId",
                                conn);
                            deleteAssignmentCmd.Parameters.AddWithValue("@taskId", taskId);
                            deleteAssignmentCmd.Parameters.AddWithValue("@workerId", workerId.Value);
                            deleteAssignmentCmd.ExecuteNonQuery();
                        }

                        // 3. Удаляем все связи задачи с навыками
                        var deleteSkillsCmd = new NpgsqlCommand(
                            "DELETE FROM task_skills WHERE task_id = @id",
                            conn);
                        deleteSkillsCmd.Parameters.AddWithValue("@id", taskId);
                        deleteSkillsCmd.ExecuteNonQuery();

                        // 4. Удаляем саму задачу
                        var deleteTaskCmd = new NpgsqlCommand(
                            "DELETE FROM tasks WHERE id = @id",
                            conn);
                        deleteTaskCmd.Parameters.AddWithValue("@id", taskId);
                        deleteTaskCmd.ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                    }
                }
            }
        }
    }
}
