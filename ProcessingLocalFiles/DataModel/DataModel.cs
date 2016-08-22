using System.Data.Common;
using System.Data.Entity;

namespace DataModel
{
	public class DBModel:DbContext
	{
		public DBModel(string con):base(con)
		{
			Database.SetInitializer<DBModel>(new CreateDatabaseIfNotExists<DBModel>());
		}

		public virtual DbSet<FilesCopy> FilesCopy { get; set; }
	}
}
