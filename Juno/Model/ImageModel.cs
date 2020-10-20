﻿using System.ComponentModel.DataAnnotations;

namespace Juno.Model
{
    public class ImageModel
    {
        [Required]
        public string ImageId { get; set; }
        [Required]
        public string FileName { get; set; }
        public string Title { get; set; }
    }
}
