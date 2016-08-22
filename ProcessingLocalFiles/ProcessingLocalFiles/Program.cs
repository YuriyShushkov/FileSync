using DataModel;
using System;
using System.Collections.Concurrent;
using System.Data.SqlServerCe;
using System.IO;
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
		static int PACKSIZE2SAVE = 100;
		static string RootDir = "D:\\";


		static void Main(string[] args)
		{

			File.Delete("Test.sdf");
			string connString = "Data Source='Test.sdf'; LCID=1033;   Password=123; Encrypt = TRUE;";
			SqlCeEngine engine = new SqlCeEngine(connString);
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
			dirs.Enqueue("D:\\");
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
				DBModel.FilesCopy.Add(file);
			}
			DBModel.SaveChanges();
		}

		static FilesCopy ComputeMD5Checksum(string path)
		{
			using (FileStream fs = File.OpenRead(path))
			{
				MD5 md5 = new MD5CryptoServiceProvider();
				byte[] fileData = new byte[fs.Length];
				fs.Read(fileData, 0, (int)fs.Length);
				byte[] checkSum = md5.ComputeHash(fileData);
				string result = BitConverter.ToString(checkSum).Replace("-", string.Empty);
				return new FilesCopy{CRC=result, FileName=path.Remove(0, RootDir.Length), Length = fs.Length };
			}
		}
	}
}
