﻿using App.Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Business.Abstract
{
    public interface ICategoryService
    {
        Task<List<Category>> GetList();
    }
}
