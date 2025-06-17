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
    public partial class AddWorkerForm : Form
    {
        public AddWorkerForm()
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
                        // Добавление работника
                        var cmd = new NpgsqlCommand(@"
                        INSERT INTO workers (last_name, first_name, middle_name, email) 
                        VALUES (@ln, @fn, @mn, @email) RETURNING id", conn);
                        cmd.Parameters.AddWithValue("@ln", txtLastName.Text);
                        cmd.Parameters.AddWithValue("@fn", txtFirstName.Text);
                        cmd.Parameters.AddWithValue("@mn", txtMiddleName.Text);
                        cmd.Parameters.AddWithValue("@email", txtEmail.Text);
                        int workerId = (int)cmd.ExecuteScalar();

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

                            // Связь работника с навыком
                            cmd = new NpgsqlCommand(@"
                            INSERT INTO worker_skills (worker_id, skill_id) 
                            VALUES (@wid, @sid)", conn);
                            cmd.Parameters.AddWithValue("@wid", workerId);
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
