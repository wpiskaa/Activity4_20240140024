-- ============================================================
-- Modul 14: Update & Create Stored Procedures
-- ============================================================

-- 1. Update sp_GetMahasiswa (tambah kolom Foto)
IF OBJECT_ID('dbo.sp_GetMahasiswa', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetMahasiswa;
GO

CREATE PROCEDURE sp_GetMahasiswa
AS
BEGIN
    SELECT NIM, Nama, JenisKelamin, TanggalLahir, Alamat, KodeProdi, TanggalDaftar, Foto
    FROM Mahasiswa;
END
GO

-- 2. Update sp_InsertMahasiswa (tambah parameter @Foto)
IF OBJECT_ID('dbo.sp_InsertMahasiswa', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_InsertMahasiswa;
GO

CREATE PROCEDURE sp_InsertMahasiswa
    @NIM          CHAR(11),
    @Nama         VARCHAR(100),
    @JenisKelamin CHAR(1),
    @TanggalLahir DATE,
    @Alamat       VARCHAR(200),
    @KodeProdi    CHAR(10),
    @TanggalDaftar DATETIME,
    @Foto         VARBINARY(MAX) = NULL
AS
BEGIN
    INSERT INTO Mahasiswa (NIM, Nama, JenisKelamin, TanggalLahir, Alamat, KodeProdi, TanggalDaftar, Foto)
    VALUES (@NIM, @Nama, @JenisKelamin, @TanggalLahir, @Alamat, @KodeProdi, @TanggalDaftar, @Foto);
END
GO

-- 3. Update sp_UpdateMahasiswa (tambah parameter @Foto)
IF OBJECT_ID('dbo.sp_UpdateMahasiswa', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UpdateMahasiswa;
GO

CREATE PROCEDURE sp_UpdateMahasiswa
    @NIM          CHAR(11),
    @Nama         VARCHAR(100),
    @JenisKelamin CHAR(1),
    @TanggalLahir DATE,
    @Alamat       VARCHAR(200),
    @KodeProdi    CHAR(10),
    @Foto         VARBINARY(MAX) = NULL
AS
BEGIN
    UPDATE Mahasiswa
    SET Nama         = @Nama,
        JenisKelamin = @JenisKelamin,
        TanggalLahir = @TanggalLahir,
        Alamat       = @Alamat,
        KodeProdi    = @KodeProdi,
        Foto         = CASE WHEN @Foto IS NOT NULL THEN @Foto ELSE Foto END
    WHERE NIM = @NIM;
END
GO

-- 4. Buat sp_ChartProdi (jumlah mahasiswa per program studi)
IF OBJECT_ID('dbo.sp_ChartProdi', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ChartProdi;
GO

CREATE PROCEDURE sp_ChartProdi
AS
BEGIN
    SELECT p.NamaProdi AS Label, COUNT(m.NIM) AS Jumlah
    FROM ProgramStudi p
    LEFT JOIN Mahasiswa m ON p.KodeProdi = m.KodeProdi
    GROUP BY p.NamaProdi
    ORDER BY Jumlah DESC;
END
GO

-- 5. Buat sp_ChartTahun (jumlah mahasiswa per tahun masuk)
IF OBJECT_ID('dbo.sp_ChartTahun', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ChartTahun;
GO

CREATE PROCEDURE sp_ChartTahun
AS
BEGIN
    SELECT YEAR(TanggalDaftar) AS Label, COUNT(NIM) AS Jumlah
    FROM Mahasiswa
    GROUP BY YEAR(TanggalDaftar)
    ORDER BY Label;
END
GO
