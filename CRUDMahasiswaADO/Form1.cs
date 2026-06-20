using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CRUDMahasiswaADO
{
    public partial class Form1 : Form
    {
        // Connection String (tetap dipakai untuk Transaction yang belum dipindah ke DAL)
        private readonly string connectionString = "Data Source=PISKA\\PISKA;Initial Catalog=DBAkademikADO;Integrated Security=True";

        // DAL – Data Access Layer
        private DAL dal = new DAL();

        // Byte array untuk foto yang sedang dipilih
        private byte[] _selectedFotoBytes = null;

        // Langkah 2 – Menambahkan BindingSource & DataTable
        private BindingSource bindingSource = new BindingSource();
        private DataTable dtMahasiswa = new DataTable();

        public Form1()
        {
            InitializeComponent();
        }

        // Langkah 3 – Menambahkan Form Load
        // (tidak dipanggil Designer – logika dipindah ke Form1_Load_1)
        private void Form1_Load(object sender, EventArgs e)
        {
        }

        // Langkah 4 – Load Data menggunakan DAL
        private void LoadData()
        {
            try
            {
                dtMahasiswa = dal.GetMahasiswa();
                bindingSource.DataSource = dtMahasiswa;
                dataGridView1.DataSource = bindingSource;
                BindControls();
                HitungTotal();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal load data: " + ex.Message);
            }
        }

        // Hitung Total menggunakan DAL
        private void HitungTotal()
        {
            try
            {
                int total = dal.HitungTotal();
                lblTotal.Text = "Total Mahasiswa: " + total;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menghitung total: " + ex.Message);
            }
        }

        // Langkah 5 – Menambahkan Bind Control
        private void BindControls()
        {
            txtNIM.DataBindings.Clear();
            txtNama.DataBindings.Clear();
            cmbJK.DataBindings.Clear();
            dtpTanggalLahir.DataBindings.Clear();
            txtAlamat.DataBindings.Clear();
            txtKodeProdi.DataBindings.Clear();

            txtNIM.DataBindings.Add("Text", bindingSource, "NIM");
            txtNama.DataBindings.Add("Text", bindingSource, "Nama");
            cmbJK.DataBindings.Add("Text", bindingSource, "JenisKelamin");
            dtpTanggalLahir.DataBindings.Add("Value", bindingSource, "TanggalLahir");
            txtAlamat.DataBindings.Add("Text", bindingSource, "Alamat");
            txtKodeProdi.DataBindings.Add("Text", bindingSource, "KodeProdi");
        }

        // Langkah 6 – Connect Test
        // ===================================================
        // CONNECT TEST
        // ===================================================
        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    MessageBox.Show("Koneksi berhasil");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Koneksi gagal: " + ex.Message);
            }
        }

        // Langkah 7 – Menggunakan DataAdapter untuk Load Data
        private void btnLoad_Click(object sender, EventArgs e)
        {
            LoadData();
        }

        // INSERT via Transaction (Modul 12) + Foto (Modul 14)
        private void btnTambah_Click(object sender, EventArgs e)
        {
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            SqlTransaction trans = conn.BeginTransaction();
            try
            {
                // 1. Insert data mahasiswa (dengan foto)
                SqlCommand cmd = new SqlCommand("sp_InsertMahasiswa", conn, trans);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NIM", txtNIM.Text);
                cmd.Parameters.AddWithValue("@Nama", txtNama.Text);
                cmd.Parameters.AddWithValue("@JenisKelamin", cmbJK.Text);
                cmd.Parameters.AddWithValue("@TanggalLahir", dtpTanggalLahir.Value.Date);
                cmd.Parameters.AddWithValue("@Alamat", txtAlamat.Text);
                cmd.Parameters.AddWithValue("@KodeProdi", txtKodeProdi.Text);
                cmd.Parameters.AddWithValue("@TanggalDaftar", DateTime.Now);
                cmd.Parameters.Add("@Foto", SqlDbType.VarBinary, -1).Value =
                    (object)_selectedFotoBytes ?? DBNull.Value;
                cmd.ExecuteNonQuery();

                // 2. Log aktivitas
                SqlCommand cmdLog = new SqlCommand(
                    @"INSERT INTO LogAktivitas (aktivitas,waktu) VALUES (@aktivitas,GETDATE())",
                    conn, trans);
                cmdLog.Parameters.AddWithValue("@aktivitas", "INSERT MAHASISWA : " + txtNIM.Text);
                cmdLog.ExecuteNonQuery();

                trans.Commit();
                MessageBox.Show("Data berhasil ditambahkan");
                _selectedFotoBytes = null;
                pictureBoxFoto.Image = null;
                LoadData();
            }
            catch (SqlException ex)
            {
                trans.Rollback();
                dal.SimpanLog(ex.Message);
                dal.SimpanLogAktivitas("ROLLBACK INSERT : " + ex.Message);
                MessageBox.Show("SQL Error : " + ex.Message);
            }
            catch (Exception ex)
            {
                trans.Rollback();
                dal.SimpanLog(ex.Message);
                dal.SimpanLogAktivitas("GENERAL ERROR : " + ex.Message);
                MessageBox.Show("General Error : " + ex.Message);
            }
            finally { conn.Close(); }
        }

        // ===================================================
        // HELPER: delegate ke DAL (backward compat)
        // ===================================================
        private void SimpanLog(string pesan) => dal.SimpanLog(pesan);
        private void SimpanLogAktivitasSalah(string pesan) => dal.SimpanLogAktivitas(pesan);

        // UPDATE via DAL (dengan foto opsional)
        private void btnUbah_Click(object sender, EventArgs e)
        {
            try
            {
                dal.UpdateMahasiswa(
                    txtNIM.Text, txtNama.Text, cmbJK.Text,
                    dtpTanggalLahir.Value, txtAlamat.Text, txtKodeProdi.Text,
                    _selectedFotoBytes);

                MessageBox.Show("Data berhasil diupdate");
                _selectedFotoBytes = null;
                LoadData();
            }
            catch (SqlException ex) { SimpanLog(ex.Message); MessageBox.Show("SQL Error : " + ex.Message); }
            catch (Exception ex)    { SimpanLog(ex.Message); MessageBox.Show("General Error : " + ex.Message); }
        }

        // DELETE via Stored Procedure (sp_DeleteMahasiswa)
        private void btnDelate_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult konfirmasi = MessageBox.Show(
                    "Yakin ingin menghapus data ini?",
                    "Konfirmasi Hapus",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (konfirmasi == DialogResult.Yes)
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_DeleteMahasiswa", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add("@NIM", SqlDbType.Char, 11).Value = txtNIM.Text;

                            conn.Open();
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                                MessageBox.Show("Data berhasil dihapus");
                            else
                                MessageBox.Show("Data tidak ditemukan");
                        }
                    }

                    LoadData();
                }
            }
            catch (SqlException ex)
            {
                SimpanLog(ex.Message);
                MessageBox.Show("SQL Error : " + ex.Message);
            }
            catch (Exception ex)
            {
                SimpanLog(ex.Message);
                MessageBox.Show("General Error : " + ex.Message);
            }
        }

        // Langkah 9 – Reset Data dari Backup
        private void btnResetData_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                        IF OBJECT_ID('dbo.Mahasiswa_Backup') IS NOT NULL
                        BEGIN
                            DELETE FROM dbo.Mahasiswa;
                            INSERT INTO dbo.Mahasiswa
                            SELECT * FROM dbo.Mahasiswa_Backup;
                        END";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Data berhasil direset");
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Reset gagal: " + ex.Message);
            }
        }

        // Langkah 10 – Simulasi SQL Injection (TIDAK AMAN – hanya untuk demo)
        // Modul Praktikum 11 – Menguji Trigger trg_PreventMassUpdate
        private void btnTestInjection_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "UPDATE Mahasiswa SET Nama='" + txtNama.Text + "' WHERE NIM='" + txtNIM.Text + "'";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Update berhasil");
                }
            }
            catch (SqlException ex)
            {
                SimpanLog(ex.Message);
                MessageBox.Show("SQL Error : " + ex.Message);
            }
            catch (Exception ex)
            {
                SimpanLog(ex.Message);
                MessageBox.Show("General Error : " + ex.Message);
            }
            LoadData();
        }

        // ===================================================
        // Event handler Form Load
        // ===================================================
        private void Form1_Load_1(object sender, EventArgs e)
        {
            cmbJK.DataSource = new string[] { "L", "P" };

            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            bindingNavigator1.BindingSource = bindingSource;

            // Event seleksi baris grid → tampilkan foto
            dataGridView1.SelectionChanged += DataGridView1_SelectionChanged;

            LoadData();
        }

        // ===================================================
        // Tampilkan foto saat baris grid dipilih
        // ===================================================
        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;
            try
            {
                object fotoObj = dataGridView1.CurrentRow.Cells["Foto"]?.Value;
                if (fotoObj != null && fotoObj != DBNull.Value)
                {
                    byte[] fotoBytes = (byte[])fotoObj;
                    using (var ms = new MemoryStream(fotoBytes))
                        pictureBoxFoto.Image = Image.FromStream(ms);
                }
                else
                {
                    pictureBoxFoto.Image = null;
                }
            }
            catch { pictureBoxFoto.Image = null; }
        }

        // ===================================================
        // Upload Foto – buka dialog pilih gambar
        // ===================================================
        private void btnUploadFoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Pilih Foto Mahasiswa";
                dlg.Filter = "Gambar|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _selectedFotoBytes = File.ReadAllBytes(dlg.FileName);
                    pictureBoxFoto.Image = Image.FromFile(dlg.FileName);
                }
            }
        }

        // ===================================================
        // Import Excel – buka file xlsx dan import ke DB
        // ===================================================
        private void btnImportExcel_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Pilih File Excel";
                dlg.Filter = "Excel Files|*.xlsx;*.xls";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                try
                {
                    var (berhasil, gagal, pesanGagal) = dal.ImportFromExcel(dlg.FileName);
                    string msg = $"Import selesai!\n✅ Berhasil: {berhasil} baris\n❌ Gagal: {gagal} baris";
                    if (!string.IsNullOrEmpty(pesanGagal))
                        msg += "\n\nDetail gagal:\n" + pesanGagal;
                    MessageBox.Show(msg, "Hasil Import", MessageBoxButtons.OK,
                        gagal > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Gagal import Excel: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void txtNama_TextChanged(object sender, EventArgs e)
        {
        }

        private void txtNIM_TextChanged(object sender, EventArgs e)
        {
        }

        private void lblTotal_Click(object sender, EventArgs e)
        {

        }

        private void btnRekap_Click(object sender, EventArgs e)
        {
                    RekapMahasiswa fm3 = new RekapMahasiswa();
            fm3.Show();
            this.Hide();
        }
    }
}