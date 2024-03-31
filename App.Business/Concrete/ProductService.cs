using App.Business.Abstract;
using App.DataAccess.Abstract;
using App.DataAccess.Concrete.EfEntityFramework;
using App.Entities.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Business.Concrete
{
    public class ProductService : IProductService
    {
        private readonly IProductDal _productDal;

        public ProductService(IProductDal productDal)
        {
            _productDal = productDal;
        }

        public ProductService()
        {
            _productDal = new EfProductDal();
        }

        public async Task Add(Product product)
        {
            await _productDal.Add(product);
        }

        public async Task Delete(Product product)
        {
            await _productDal.Delete(product);
        }

        public async Task<List<Product>> GetAll()
        {
            return await _productDal.GetList();
        }

        public async Task<List<Product>> GetByCategory(int categoryId)
        {
            return await _productDal.GetList(p=>p.CategoryId==categoryId);
        }

        public async Task<Product> GetById(int productId)
        {
            return await _productDal.Get(p=>p.ProductId==productId);
        }

        public async Task Update(Product product)
        {
            await _productDal.Update(product);
        }
    }
}
