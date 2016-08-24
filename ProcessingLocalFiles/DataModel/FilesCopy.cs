using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataModel
{
	public class FilesCopy
	{
		[Key]
		[Display(Name = "Индетификатор файла")]
		public Guid Id { get; set; }

		[Display(Name = "Путь файла")]
		[Index("FileName", IsUnique = true)]		
		public string FileName { get; set; }

		[Display(Name = "Контрольная сумма")]
		public string CRC { get; set; }

		[Display(Name = "Размер файла")]
		public long Length { get; set; }

		[Display(Name = "Дата создания")]
		public DateTime CreateDate { get; set; }

		[Display(Name = "Дата изменения")]
		public DateTime WriteDate { get; set; }

		[Display(Name = "Индетификатор источника файла")]
		[Index("Source", IsUnique = true, Order = 1)]
		public Guid Source { get; set; }

		[Display(Name = "Индетификатор файла в источнике")]
		[Index("Source", IsUnique = true, Order = 2)]
		public Guid SourceId { get; set; }
	}
}
