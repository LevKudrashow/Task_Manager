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
    public partial class AddTaskForm : Form
    {
        public AddTaskForm()
        {
            InitializeComponent();
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
                        // Добавление задачи
                        var cmd = new NpgsqlCommand(@"
                        INSERT INTO tasks (title, description) 
                        VALUES (@tl, @ds) RETURNING id", conn);
                        cmd.Parameters.AddWithValue("@tl", txtTitle.Text);
                        cmd.Parameters.AddWithValue("@ds", txtDesc.Text);
                        int taskId = (int)cmd.ExecuteScalar();

                        // Обработка навыков
                        var skills = txtSkills.Text.Split(' ');
                        foreach (var skill in skills)
                        {
                            // Добавление навыка если не существует
                            cmd = new NpgsqlCommand(@"
                            INSERT INTO skills (name) 
                            VALUES (@name) 
                            ON CONFLICT (name) DO UPDATE SET name = EXCLUDED.name 
                            RETURNING id", conn);
                            cmd.Parameters.AddWithValue("@name", skill);
                            int skillId = (int)cmd.ExecuteScalar();

                            // Связь задачи с навыком
                            cmd = new NpgsqlCommand(@"
                            INSERT INTO task_skills (task_id, skill_id) 
                            VALUES (@tid, @sid)", conn);
                            cmd.Parameters.AddWithValue("@tid", taskId);
                            cmd.Parameters.AddWithValue("@sid", skillId);
                            cmd.ExecuteNonQuery();
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
