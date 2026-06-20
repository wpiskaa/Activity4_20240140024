using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using ExcelDataReader;

namespace CRUDMahasiswaADO
{
    /// <summary>
    /// Data Access Layer – berisi semua logika database.
    /// </summary>
    public class DAL
    {
        // --------------------------------------------------------
        // Connection String (bisa diganti ke IP public satu jaringan)
        // --------------------------------------------------------
        private string connectionString = GetConnectionString();

        private static string GetConnectionString()
        {
            // Gunakan nama server statis; bisa diubah ke IP untuk deploy
            return "Data Source=PISKA\\PISKA;Initial Catalog=DBAkademikADO;Integrated Security=True";
        }

        /// <summary>
        /// Mendapatkan IP Address lokal mesin ini (untuk deploy satu jaringan).
        /// </summary>
        public static string GetIPAddress()
        {
            try
            {
                string hostName = Dns.GetHostName();
                foreach (IPAddress ip in Dns.GetHostAddresses(hostName))
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return ip.ToString();
                }
            }
            catch { }
            return "127.0.0.1";
        }

        // --------------------------------------------------------
        // READ: ambil semua data mahasiswa
        // --------------------------------------------------------
        public DataTable GetMahasiswa()
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("sp_GetMahasiswa", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    da.Fill(dt);
            }
            return dt;
        }

        // --------------------------------------------------------
        // COUNT: hitung total mahasiswa (OUTPUT param)
        // --------------------------------------------------------
        public int HitungTotal()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("sp_CountMahasiswa", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                SqlParameter outParam = new SqlParameter("@Total", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(outParam);
                conn.Open();
                cmd.ExecuteNonQuery();
                return (int)outParam.Value;
            }
        }

        // --------------------------------------------------------
        // INSERT mahasiswa (dengan foto opsional)
        // --------------------------------------------------------
        public void InsertMahasiswa(string nim, string nama, string jk,
            DateTime tglLahir, string alamat, string kodeProdi,
            DateTime tglDaftar, byte[] foto = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("sp_InsertMahasiswa", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NIM", nim);
                cmd.Parameters.AddWithValue("@Nama", nama);
                cmd.Parameters.AddWithValue("@JenisKelamin", jk);
                cmd.Parameters.AddWithValue("@TanggalLahir", tglLahir.Date);
                cmd.Parameters.AddWithValue("@Alamat", alamat);
                cmd.Parameters.AddWithValue("@KodeProdi", kodeProdi);
                cmd.Parameters.AddWithValue("@TanggalDaftar", tglDaftar);
                cmd.Parameters.Add("@Foto", SqlDbType.VarBinary, -1).Value =
                    (object)foto ?? DBNull.Value;
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // --------------------------------------------------------
        // UPDATE mahasiswa (foto = null → tidak ubah foto lama)
        // --------------------------------------------------------
        public void UpdateMahasiswa(string nim, string nama, string jk,
            DateTime tglLahir, string alamat, string kodeProdi, byte[] foto = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("sp_UpdateMahasiswa", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NIM", nim);
                cmd.Parameters.AddWithValue("@Nama", nama);
                cmd.Parameters.AddWithValue("@JenisKelamin", jk);
                cmd.Parameters.AddWithValue("@TanggalLahir", tglLahir.Date);
                cmd.Parameters.AddWithValue("@Alamat", alamat);
                cmd.Parameters.AddWithValue("@KodeProdi", kodeProdi);
                cmd.Parameters.Add("@Foto", SqlDbType.VarBinary, -1).Value =
                    (object)foto ?? DBNull.Value;
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // --------------------------------------------------------
        // DELETE mahasiswa
        // --------------------------------------------------------
        public void DeleteMahasiswa(string nim)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand("sp_DeleteMahasiswa", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@NIM", SqlDbType.Char, 11).Value = nim;
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // --------------------------------------------------------
        // CHART: data per prodi atau per tahun
        // --------------------------------------------------------
        public DataTable GetChartData(string tipe)
        {
            DataTable dt = new DataTable();
            string spName = tipe == "Per Program Studi" ? "sp_ChartProdi" : "sp_ChartTahun";
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(spName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    da.Fill(dt);
            }
            return dt;
        }

        // --------------------------------------------------------
        // LOG ERROR
        // --------------------------------------------------------
        public void SimpanLog(string pesan)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(
                    "INSERT INTO LogError (waktu, pesan_error) VALUES (GETDATE(), @pesan)", conn))
                {
                    cmd.Parameters.AddWithValue("@pesan", pesan);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* jangan lempar exception baru */ }
        }

        // --------------------------------------------------------
        // LOG AKTIVITAS
        // --------------------------------------------------------
        public void SimpanLogAktivitas(string aktivitas)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand(
                    "INSERT INTO LogAktivitas (aktivitas, waktu) VALUES (@aktivitas, GETDATE())", conn))
                {
                    cmd.Parameters.AddWithValue("@aktivitas", aktivitas);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        // --------------------------------------------------------
        // IMPORT EXCEL → Insert ke database
        // CATATAN: Membutuhkan NuGet package "ExcelDataReader" dan
        // "ExcelDataReader.DataSet". Install dulu via Manage NuGet Packages.
        // --------------------------------------------------------
        public (int berhasil, int gagal, string pesanGagal) ImportFromExcel(string filePath)
        {
#if true
            int berhasil = 0, gagal = 0;
            string pesanGagal = "";

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                IExcelDataReader reader = filePath.EndsWith(".xlsx")
                    ? ExcelReaderFactory.CreateOpenXmlReader(stream)
                    : ExcelReaderFactory.CreateBinaryReader(stream);

                var config = new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
                };

                DataSet ds = reader.AsDataSet(config);
                reader.Close();

                if (ds.Tables.Count == 0) return (0, 0, "File Excel kosong");

                DataTable dt = ds.Tables[0];
                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        string nim          = dt.Columns.Contains("NIM") ? row["NIM"]?.ToString().Trim() : null;
                        string nama         = dt.Columns.Contains("Nama") ? row["Nama"]?.ToString().Trim() : null;
                        string jk           = dt.Columns.Contains("JenisKelamin") ? row["JenisKelamin"]?.ToString().Trim() : null;
                        string tglLahirStr  = dt.Columns.Contains("TanggalLahir") ? row["TanggalLahir"]?.ToString().Trim() : null;
                        string alamat       = dt.Columns.Contains("Alamat") ? row["Alamat"]?.ToString().Trim() : null;

                        if (string.IsNullOrEmpty(nim)) continue;

                        // Cek jika NIM sudah terdaftar, jika ya kita lewati (skip) agar tidak error duplicate key
                        if (ApakahMahasiswaAda(nim))
                        {
                            continue;
                        }

                        string kodeProdi = null;
                        if (dt.Columns.Contains("KodeProdi"))
                        {
                            kodeProdi = row["KodeProdi"]?.ToString().Trim();
                        }
                        else if (dt.Columns.Contains("NamaProdi"))
                        {
                            string namaProdi = row["NamaProdi"]?.ToString().Trim();
                            if (namaProdi == "Teknik Informatika") kodeProdi = "TI01";
                            else if (namaProdi == "Sistem Informasi") kodeProdi = "SI01";
                            else if (namaProdi == "Manajemen Informatika") kodeProdi = "MI01";
                            else kodeProdi = "TI01"; // default fallback
                        }
                        else
                        {
                            kodeProdi = "TI01"; // default fallback
                        }

                        string tglDaftarStr = dt.Columns.Contains("TanggalDaftar") ? row["TanggalDaftar"]?.ToString().Trim() : null;

                        DateTime tglLahir  = DateTime.TryParse(tglLahirStr, out var tl)  ? tl  : DateTime.Today;
                        DateTime tglDaftar = DateTime.TryParse(tglDaftarStr, out var td) ? td : DateTime.Now;

                        InsertMahasiswa(nim, nama, jk, tglLahir, alamat, kodeProdi, tglDaftar);
                        berhasil++;
                    }
                    catch (Exception ex)
                    {
                        gagal++;
                        string nimVal = dt.Columns.Contains("NIM") ? row["NIM"]?.ToString() : "?";
                        pesanGagal += $"Baris NIM {nimVal}: {ex.Message}\n";
                    }
                }
            }
            return (berhasil, gagal, pesanGagal);
#else
            // Stub: tampil pesan sampai ExcelDataReader terinstall
            throw new InvalidOperationException(
                "Package ExcelDataReader belum terinstall.\n" +
                "Install via: Tools > NuGet Package Manager > Manage NuGet Packages\n" +
                "Cari: ExcelDataReader dan ExcelDataReader.DataSet");
#endif
        }

        // --------------------------------------------------------
        // FOTO: ambil foto mahasiswa dari DB sebagai Image
        // --------------------------------------------------------
        public Image GetFotoMahasiswa(string nim)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT Foto FROM Mahasiswa WHERE NIM = @NIM", conn))
            {
                cmd.Parameters.AddWithValue("@NIM", nim);
                conn.Open();
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    byte[] bytes = (byte[])result;
                    using (var ms = new MemoryStream(bytes))
                        return Image.FromStream(ms);
                }
            }
            return null;
        }

        // --------------------------------------------------------
        // CEK APAKAH MAHASISWA ADA (untuk skip duplikasi saat import)
        // --------------------------------------------------------
        public bool ApakahMahasiswaAda(string nim)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Mahasiswa WHERE NIM = @NIM", conn))
                {
                    cmd.Parameters.AddWithValue("@NIM", nim);
                    conn.Open();
                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        // --------------------------------------------------------
        // LIST PRODI untuk ComboBox
        // --------------------------------------------------------
        public DataTable GetProgramStudi()
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT KodeProdi, NamaProdi FROM ProgramStudi", conn))
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                da.Fill(dt);
            return dt;
        }
    }
}
