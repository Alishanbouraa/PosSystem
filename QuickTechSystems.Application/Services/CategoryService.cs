using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.Domain.Entities;
using QuickTechSystems.Domain.Interfaces.Repositories;

namespace QuickTechSystems.Application.Services
{
    public class CategoryService : BaseService<Category, CategoryDTO>, ICategoryService
    {
        public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
            : base(unitOfWork, mapper, unitOfWork.Categories)
        {
        }

        public async Task<CategoryDTO?> GetByNameAsync(string name)
        {
            var categories = await _repository.Query()
                .Where(c => c.Name.Contains(name))
                .FirstOrDefaultAsync();
            return _mapper.Map<CategoryDTO>(categories);
        }

        public async Task<IEnumerable<CategoryDTO>> GetActiveAsync()
        {
            var categories = await _repository.Query()
                .Where(c => c.IsActive)
                .ToListAsync();
            return _mapper.Map<IEnumerable<CategoryDTO>>(categories);
        }
    }
}