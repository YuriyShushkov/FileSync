using System.ComponentModel.DataAnnotations.Schema;

namespace DataModel
{
	public class FilesCopy
	{
		[Column(Order = 2)]
		public string FileName { get; set; }

		[Column(Order = 2)]
		public string CRC { get; set; }

		[Column(Order = 3)]
		public long Length { get; set; }
	}
}
