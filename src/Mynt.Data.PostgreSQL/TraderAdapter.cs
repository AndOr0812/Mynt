﻿ using System;
using System.ComponentModel.DataAnnotations;

namespace Mynt.Data.PostgreSQL
{
    public class TraderAdapter
    {
        [Key]
        public int Id { get; set; }

        public string Identifier { get; set; }
        public double StakeAmount { get; set; }
        public double CurrentBalance { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsBusy { get; set; }
        public bool IsArchived { get; set; }
    }
}
