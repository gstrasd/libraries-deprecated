using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.ContentDelivery.BunnyCdn
{
    public class StorageObject
    {
        public string Guid { get; set; }
        public string UserId { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime LastChanged { get; set; }
        public string StorageZoneName { get; set; }
        public string Path { get; set; }
        public string ObjectName { get; set; }
        public long Length { get; set; }
        public bool IsDirectory { get; set; }
        public int ServerId { get; set; }
        public long StorageZoneId { get; set; }
        public string FullPath => Path + ObjectName;
    }
}