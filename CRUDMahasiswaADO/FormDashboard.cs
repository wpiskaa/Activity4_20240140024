using System;
using System.Data;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace CRUDMahasiswaADO
{
    public partial class FormDashboard : Form
    {
        private DAL dal = new DAL();

        public FormDashboard()
        {
            InitializeComponent();
        }

        private void FormDashboard_Load(object sender, EventArgs e)
        {
            // Isi pilihan tipe chart
            cmbTipe.Items.AddRange(new string[] { "Per Program Studi", "Per Tahun Masuk" });
            cmbTipe.SelectedIndex = 0;

            // Load chart default
            LoadDataChart();
        }

        // --------------------------------------------------------
        // Load data ke Chart
        // --------------------------------------------------------
        private void LoadDataChart()
        {
            try
            {
                string tipe = cmbTipe.SelectedItem?.ToString() ?? "Per Program Studi";
                DataTable dt = dal.GetChartData(tipe);

                chartMahasiswa.Series.Clear();
                chartMahasiswa.Titles.Clear();

                Series series = new Series("Jumlah Mahasiswa");
                series.ChartType = SeriesChartType.Column;

                foreach (DataRow row in dt.Rows)
                {
                    string label  = row["Label"]?.ToString();
                    int    jumlah = Convert.ToInt32(row["Jumlah"]);
                    int    idx    = series.Points.AddXY(label, jumlah);
                    series.Points[idx].Label = jumlah.ToString();
                }

                chartMahasiswa.Series.Add(series);
                chartMahasiswa.Titles.Add("Grafik Mahasiswa " + tipe);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal load chart: " + ex.Message);
            }
        }

        // --------------------------------------------------------
        // Event: ComboBox tipe berubah
        // --------------------------------------------------------
        private void cmbTipe_SelectedValueChanged(object sender, EventArgs e)
        {
            LoadDataChart();
        }

        // --------------------------------------------------------
        // Button: Load (refresh chart)
        // --------------------------------------------------------
        private void btnLoad_Click(object sender, EventArgs e)
        {
            LoadDataChart();
        }

        // --------------------------------------------------------
        // Button: Reset (kembali ke pilihan pertama)
        // --------------------------------------------------------
        private void btnReset_Click(object sender, EventArgs e)
        {
            cmbTipe.SelectedIndex = 0;
            LoadDataChart();
        }

        // --------------------------------------------------------
        // Button: Buka Form Data Mahasiswa (Form1)
        // --------------------------------------------------------
        private void btnDataMahasiswa_Click(object sender, EventArgs e)
        {
            Form1 form1 = new Form1();
            form1.FormClosed += (s, args) => this.Show();
            form1.Show();
            this.Hide();
        }
    }
}
