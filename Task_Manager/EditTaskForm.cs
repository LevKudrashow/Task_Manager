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
    public partial class EditTaskForm : Form
    {
        private int _taskId;
        private bool wasBusy;
        public EditTaskForm(int taskId)
        {
            _taskId = taskId;
            InitializeComponent();
            LoadTaskData();
        }

        private void LoadTaskData()
        {
            using (var conn = DBHelper.GetConnection())
            {
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM tasks WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", _taskId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        txtTitle.Text = reader["title"].ToString();
                        txtDesc.Text = reader["description"].ToString();
                        chkIsBusy.Checked = Convert.ToBoolean(reader["is_busy"]);
                        wasBusy = chkIsBusy.Checked;
                    }
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            using (var conn = DBHelper.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Обновление данных задачи
                        var cmd = new NpgsqlCommand(@"
                        UPDATE tasks 
                        SET title = @tl, description = @ds, is_busy = @busy 
                        WHERE id = @id", conn);
                        cmd.Parameters.AddWithValue("@id", _taskId);
                        cmd.Parameters.AddWithValue("@tl", txtTitle.Text);
                        cmd.Parameters.AddWithValue("@ds", txtDesc.Text);
                        cmd.Parameters.AddWithValue("@busy", chkIsBusy.Checked);
                        cmd.ExecuteNonQuery();

                        // Удаление старых навыков
                        cmd = new NpgsqlCommand("DELETE FROM task_skills WHERE task_id = @id", conn);
                        cmd.Parameters.AddWithValue("@id", _taskId);
                        cmd.ExecuteNonQuery();

                        // Добавление новых навыков
                        var skills = txtSkills.Text.Split(' ');
                        foreach (var skill in skills)
                        {
                            cmd = new NpgsqlCommand(@"
                            INSERT INTO skills (name) 
                            VALUES (@name) 
                            ON CONFLICT (name) DO UPDATE SET name = EXCLUDED.name 
                            RETURNING id", conn);
                            cmd.Parameters.AddWithValue("@name", skill);
                            int skillId = (int)cmd.ExecuteScalar();

                            cmd = new NpgsqlCommand(@"
                            INSERT INTO task_skills (task_id, skill_id) 
                            VALUES (@tid, @sid)", conn);
                            cmd.Parameters.AddWithValue("@tid", _taskId);
                            cmd.Parameters.AddWithValue("@sid", skillId);
                            cmd.ExecuteNonQuery();
                        }
                        // Если снимаем флаг занятости
                        if (wasBusy && !chkIsBusy.Checked)
                        {
                            // Находим связанного работника
                            var findWorkerCmd = new NpgsqlCommand(
                                "SELECT worker_id FROM assignments WHERE task_id = @id",
                                conn);
                            findWorkerCmd.Parameters.AddWithValue("@id", _taskId);
                            var workerId = findWorkerCmd.ExecuteScalar() as int?;

                            if (workerId.HasValue)
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
                                deleteAssignmentCmd.Parameters.AddWithValue("@taskId", _taskId);
                                deleteAssignmentCmd.Parameters.AddWithValue("@workerId", workerId.Value);
                                deleteAssignmentCmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка: {ex.Message}");
                    }
                }
            }
        }
    }
}
