﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Breeze.AspNetCore.EFCore.Sample.Models
{
    public class Course
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CourseID { get; set; }
        public string Title { get; set; }
        public int Credits { get; set; }

        public ICollection<Enrollment> Enrollments { get; set; }
    }
}
