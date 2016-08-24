using DataModel;
using System;
using System.Collections.Concurrent;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Timers;

namespace ProcessingLocalFiles
{
	class Program
	{
		static ConcurrentQueue<string> files = new ConcurrentQueue<string>();
		static ConcurrentQueue<string> dirs = new ConcurrentQueue<string>();
		static ConcurrentQueue<FilesCopy> content = new ConcurrentQueue<FilesCopy>();
		static string FILE_PETTERN = "*.*";
		static TimeSpan StartFindInterval = new TimeSpan(0,0,0,0,1);
		static TimeSpan StartAnaliteInterval = new TimeSpan(0, 0, 0, 0, 5);
		static TimeSpan SaveInterval = new TimeSpan(0, 0, 0, 15, 0);
		static DBModel DBModel ;
		static Guid CurSource = new Guid("babcf50f-1bcd-41d0-96d6-33c38c1cd960");
		static int PACKSIZE2SAVE = 100;
		static string RootDir = @"d:\Shared\";
		static string DB = "Test.sdf";

		static void Main(string[] args)
		{
			string connString = $"Data Source='{DB}'; LCID=1033;   Password=123; Encrypt = TRUE;";
			SqlCeEngine engine = new SqlCeEngine(connString);
			if (!File.Exists(DB))
				engine.CreateDatabase();
						
			DBModel = new DBModel(engine.LocalConnectionString);

			dirs.Enqueue(RootDir);
			// Таймер обхода директорий
			StartTimer(FillDirs, StartFindInterval.TotalMilliseconds);

			// Таймер анализа файлов
			StartTimer(AnalyteFiles, StartAnaliteInterval.TotalMilliseconds);

			// Таймер анализа файлов
			StartTimer(SaveData, SaveInterval.TotalMilliseconds);

			Console.ReadKey();
			dirs.Enqueue(RootDir);
			Console.ReadKey();
		}

		/// <summary>
		/// Старт таймера
		/// </summary>
		/// <param name="ElapsedEvent">Обработчик таймера</param>
		/// <param name="TimerInterval">Периодичность запуска таймера</param>
		/// <param name="AutoReset">Рестартовать таймер</param>
		static void StartTimer(ElapsedEventHandler ElapsedEvent, double TimerInterval, bool AutoReset = true)
		{
			Timer timer = new Timer();
			timer.Elapsed += ElapsedEvent;
			timer.Interval = TimerInterval;
			timer.AutoReset = AutoReset;
			timer.Start();
		}

		static void FillDirs(object sender, EventArgs e)
		{
			string dir;
			dirs.TryDequeue(out dir);
			//Console.WriteLine($"Парсинг директории {dir}, в очереди {dirs.Count} директорий файлов {files.Count}");
			if (!String.IsNullOrEmpty(dir))		// Только если в списке есть записи
			{				
				try
				{
					string[] subDirs = Directory.GetDirectories(dir);
					//Console.WriteLine($"Найдено {subDirs.Length} директорий");
					for (int i = 0; i < subDirs.Length; i++)
					{
						dirs.Enqueue(subDirs[i]);
					}

					string[] fileInDir = Directory.GetFiles(dir, FILE_PETTERN);
					//Console.WriteLine($"Найдено {fileInDir.Length} файлов");
					for (int i = 0; i < fileInDir.Length; i++)
					{
						files.Enqueue(fileInDir[i]);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"При парсинге директории {dir} произошла ошибка {ex.Message}");
				}

			}
		}

		static void AnalyteFiles(object sender, EventArgs e)
		{
			if (files.Count > 0)
			{
				string file;
				files.TryDequeue(out file);
				Console.WriteLine($"Анализ файла {file}, в очереди {files.Count}");
				if (!string.IsNullOrEmpty(file))     // Только если в списке есть записи
				{
					try
					{
						var FilesCopy = ComputeMD5Checksum(file);
						content.Enqueue(FilesCopy);

					}
					catch (Exception ex)
					{
						Console.WriteLine($"При анализа файла {file} произошла ошибка {ex.Message}");
					}

				}
			}
		}

		static void SaveData(object sender, EventArgs e)
		{
			FilesCopy file;
			int i = PACKSIZE2SAVE;
			while (content.TryDequeue(out file) && i-- > 0)
			{
				var finded = DBModel.FilesCopy.FirstOrDefault(x=>x.FileName == file.FileName);
				if(finded != null)
				{
					if(finded.CRC != file.CRC || finded.Length != file.Length)
					{
						finded.CRC			= file.CRC;
						finded.Length		= file.Length;
						finded.CreateDate	= file.CreateDate;
						finded.WriteDate	= file.WriteDate;
					}
				}else
				{
					DBModel.FilesCopy.Add(file);
				}
			}
			DBModel.SaveChanges();
		}

		static FilesCopy ComputeMD5Checksum(string path)
		{
			using (FileStream fs = File.OpenRead(path))
			{
				MD5 md5 = new MD5CryptoServiceProvider();
				SHA256Managed sha = new SHA256Managed();
				byte[] checksum = sha.ComputeHash(fs);
				string result = BitConverter.ToString(checksum).Replace("-", String.Empty);
				var id = Guid.NewGuid();
				return new FilesCopy {
					CRC = result
					, FileName = path.Remove(0, RootDir.Length)
					, Length = fs.Length
					, Id = id
					, CreateDate = File.GetCreationTimeUtc(path)
					, WriteDate = File.GetLastWriteTimeUtc(path)
					, Source = CurSource
					, SourceId = id
			};
			}
		}
	}
}
