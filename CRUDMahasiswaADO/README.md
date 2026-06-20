# Tugas Praktikum PABD - Modul 14: Grafik, BLOB Gambar, Import Excel, dan Deploy Aplikasi

Tugas praktikum ini mengimplementasikan fitur-fitur baru pada aplikasi CRUD Mahasiswa menggunakan arsitektur ADO.NET:
1. **Dashboard & Grafik (Chart)**: Menampilkan visualisasi data jumlah mahasiswa per program studi dan per tahun masuk menggunakan data dynamic dari SQL Server.
2. **BLOB Gambar**: Menyimpan file foto mahasiswa langsung ke dalam database dengan tipe data `VARBINARY(MAX)` dan menampilkannya di aplikasi.
3. **Import Excel**: Membaca data mahasiswa secara massal dari berkas `.xlsx` menggunakan `ExcelDataReader` dengan pencegahan duplikasi data NIM secara otomatis.
4. **Data Access Layer (DAL)**: Memisahkan logika manipulasi database ke dalam satu berkas terpusat (`DAL.cs`).
5. **Deploy Installer**: Membungkus seluruh aset release aplikasi menjadi satu file setup tunggal menggunakan Inno Setup Compiler.

---

## 📹 Link Pengumpulan Video Praktikum:
*Silakan ganti tautan di bawah ini dengan link video Google Drive Anda:*

- 📂 [Video 1: Proses Instalasi Aplikasi (Setup)](https://drive.google.com/drive/u/0/my-drive)
- 📂 [Video 2: Demo Fungsionalitas Aplikasi](https://drive.google.com/drive/u/0/my-drive)

---

## 📁 Struktur Berkas Penting:
- **Class Logic**: [DAL.cs](DAL.cs) (Logika DB, Impor Excel)
- **Form Dashboard**: [FormDashboard.cs](FormDashboard.cs) (Visualisasi Chart)
- **Form CRUD**: [Form1.cs](Form1.cs) (Upload Foto & Integrasi DAL)
- **Desain Laporan**: [ListMahasiswa.rpt](ListMahasiswa.rpt) (Crystal Reports dengan hitungan total mahasiswa & format tanggal)
- **SQL Scripts**: [sql_modul14.sql](sql_modul14.sql) (Penciptaan tabel log dan SP chart)
