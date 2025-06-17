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
    public partial class WorkersForm : Form
    {
        public WorkersForm()
        {
            InitializeComponent();
            LoadWorkers();
        }

        private void LoadWorkers()
        {
            using (var conn = DBHelper.GetConnection())
            {
                conn.Open();
                string query = @"
                SELECT 
                    w.id, 
                    w.last_name || ' ' || w.first_name || ' ' || w.middle_name AS full_name,
                    w.email,
                    w.is_busy
                FROM workers w";

                var cmd = new NpgsqlCommand(query, conn);
                var adapter = new NpgsqlDataAdapter(cmd);
                var dt = new DataTable();
                adapter.Fill(dt);
                dataGridView.DataSource = dt;
            }
        }

        private void BtnAddWorker_Click(object sender, EventArgs e)
        {
            AddWorkerForm addForm = new AddWorkerForm();
            addForm.FormClosed += (s, args) => LoadWorkers();
            addForm.Show();
        }

        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (e.ColumnIndex == dataGridView.Columns["btnEdit"].Index)
            {
                int id = Convert.ToInt32(dataGridView.Rows[e.RowIndex].Cells["id"].Value);
                EditWorkerForm editForm = new EditWorkerForm(id);
                editForm.FormClosed += (s, args) => LoadWorkers();
                editForm.Show();
            }
            else if (e.ColumnIndex == dataGridView.Columns["btnDelete"].Index)
            {
                int id = Convert.ToInt32(dataGridView.Rows[e.RowIndex].Cells["id"].Value);
                if (MessageBox.Show("Удалить исполнителя?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    DeleteWorker(id);
                    LoadWorkers();
                }
            }
        }

        private void DeleteWorker(int workerId)
        {
            using (var conn = DBHelper.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Проверяем, занят ли работник
                        bool isBusy = false;
                        int? taskId = null;

                        var checkCmd = new NpgsqlCommand(
                            "SELECT is_busy, (SELECT task_id FROM assignments WHERE worker_id = @id) FROM workers WHERE id = @id",
                            conn);
                        checkCmd.Parameters.AddWithValue("@id", workerId);

                        using (var reader = checkCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                isBusy = reader.GetBoolean(0);
                                if (!reader.IsDBNull(1))
                                    taskId = reader.GetInt32(1);
                            }
                        }

                        // 2. Если работник занят - освобождаем задачу
                        if (isBusy && taskId.HasValue)
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
                            deleteAssignmentCmd.Parameters.AddWithValue("@workerId", workerId);
                            deleteAssignmentCmd.Parameters.AddWithValue("@taskId", taskId.Value);
                            deleteAssignmentCmd.ExecuteNonQuery();
                        }

                        // 3. Удаляем все связи работника с навыками
                        var deleteSkillsCmd = new NpgsqlCommand(
                            "DELETE FROM worker_skills WHERE worker_id = @id",
                            conn);
                        deleteSkillsCmd.Parameters.AddWithValue("@id", workerId);
                        deleteSkillsCmd.ExecuteNonQuery();

                        // 4. Удаляем самого работника
                        var deleteWorkerCmd = new NpgsqlCommand(
                            "DELETE FROM workers WHERE id = @id",
                            conn);
                        deleteWorkerCmd.Parameters.AddWithValue("@id", workerId);
                        deleteWorkerCmd.ExecuteNonQuery();

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
