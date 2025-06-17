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
    public partial class EditWorkerForm : Form
    {
        private int _workerId;
        private bool wasBusy;
        public EditWorkerForm(int workerId)
        {
            _workerId = workerId;
            InitializeComponent();
            LoadWorkerData();
        }
        private void LoadWorkerData()
        {
            using (var conn = DBHelper.GetConnection())
            {
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT * FROM workers WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", _workerId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        txtLastName.Text = reader["last_name"].ToString();
                        txtFirstName.Text = reader["first_name"].ToString();
                        txtMiddleName.Text = reader["middle_name"].ToString();
                        txtEmail.Text = reader["email"].ToString();
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
                        // Обновление данных работника
                        var cmd = new NpgsqlCommand(@"
                        UPDATE workers 
                        SET last_name = @ln, first_name = @fn, middle_name = @mn, 
                            email = @email, is_busy = @busy 
                        WHERE id = @id", conn);
                        cmd.Parameters.AddWithValue("@id", _workerId);
                        cmd.Parameters.AddWithValue("@ln", txtLastName.Text);
                        cmd.Parameters.AddWithValue("@fn", txtFirstName.Text);
                        cmd.Parameters.AddWithValue("@mn", txtMiddleName.Text);
                        cmd.Parameters.AddWithValue("@email", txtEmail.Text);
                        cmd.Parameters.AddWithValue("@busy", chkIsBusy.Checked);
                        cmd.ExecuteNonQuery();

                        // Удаление старых навыков
                        cmd = new NpgsqlCommand("DELETE FROM worker_skills WHERE worker_id = @id", conn);
                        cmd.Parameters.AddWithValue("@id", _workerId);
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
                            INSERT INTO worker_skills (worker_id, skill_id) 
                            VALUES (@wid, @sid)", conn);
                            cmd.Parameters.AddWithValue("@wid", _workerId);
                            cmd.Parameters.AddWithValue("@sid", skillId);
                            cmd.ExecuteNonQuery();
                        }

                        // Если снимаем флаг занятости
                        if (wasBusy && !chkIsBusy.Checked)
                        {
                            // Находим связанную задачу
                            var findTaskCmd = new NpgsqlCommand(
                                "SELECT task_id FROM assignments WHERE worker_id = @id",
                                conn);
                            findTaskCmd.Parameters.AddWithValue("@id", _workerId);
                            var taskId = findTaskCmd.ExecuteScalar() as int?;

                            if (taskId.HasValue)
                            {
                                // Освобождаем задачу
                                var freeTaskCmd = new NpgsqlCommand(
                                    "UPDATE tasks SET is_busy = false WHERE id = @taskId",
                                    conn);
                                freeTaskCmd.Parameters.AddWithValue("@taskId", taskId.Value);
                                freeTaskCmd.ExecuteNonQuery();

                                // Удаляем связь
                                var deleteAssignmentCmd = new NpgsqlCommand(
                                    "DELETE FROM assignments WHERE worker_id = @workerId AND task_id = @taskId",
                                    conn);
                                deleteAssignmentCmd.Parameters.AddWithValue("@workerId", _workerId);
                                deleteAssignmentCmd.Parameters.AddWithValue("@taskId", taskId.Value);
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
