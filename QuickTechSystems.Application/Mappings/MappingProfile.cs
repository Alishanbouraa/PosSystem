using AutoMapper;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Domain.Entities;

namespace QuickTechSystems.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Product, ProductDTO>()
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
                .ForMember(dest => dest.SupplierName,
                    opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : string.Empty));
            CreateMap<ProductDTO, Product>();

            CreateMap<Category, CategoryDTO>()
                .ForMember(dest => dest.ProductCount,
                    opt => opt.MapFrom(src => src.Products.Count));
            CreateMap<CategoryDTO, Category>();

            CreateMap<Customer, CustomerDTO>()
                .ForMember(dest => dest.TransactionCount,
                    opt => opt.MapFrom(src => src.Transactions.Count));
            CreateMap<CustomerDTO, Customer>();

            CreateMap<Transaction, TransactionDTO>()
                .ForMember(dest => dest.CustomerName,
                    opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : string.Empty))
                .ForMember(dest => dest.Details,
                    opt => opt.MapFrom(src => src.TransactionDetails));
            CreateMap<TransactionDTO, Transaction>()
                .ForMember(dest => dest.TransactionDetails,
                    opt => opt.MapFrom(src => src.Details));

            CreateMap<TransactionDetail, TransactionDetailDTO>()
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty))
                .ForMember(dest => dest.ProductBarcode,
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.Barcode : string.Empty))
                .ForMember(dest => dest.PurchasePrice,
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.PurchasePrice : 0));
            CreateMap<TransactionDetailDTO, TransactionDetail>()
                .ForMember(dest => dest.Product, opt => opt.Ignore());

            CreateMap<BusinessSetting, BusinessSettingDTO>();
            CreateMap<BusinessSettingDTO, BusinessSetting>();

            CreateMap<SystemPreference, SystemPreferenceDTO>();
            CreateMap<SystemPreferenceDTO, SystemPreference>();

            CreateMap<Supplier, SupplierDTO>()
                .ForMember(dest => dest.ProductCount,
                    opt => opt.MapFrom(src => src.Products.Count))
                .ForMember(dest => dest.TransactionCount,
                    opt => opt.MapFrom(src => src.Transactions.Count));
            CreateMap<SupplierDTO, Supplier>();

            CreateMap<SupplierTransaction, SupplierTransactionDTO>()
                .ForMember(dest => dest.SupplierName,
                    opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : string.Empty));
            CreateMap<SupplierTransactionDTO, SupplierTransaction>();

            CreateMap<InventoryHistory, InventoryHistoryDTO>()
                .ForMember(dest => dest.ProductName,
                    opt => opt.MapFrom(src => src.Product != null ? src.Product.Name : string.Empty));
            CreateMap<InventoryHistoryDTO, InventoryHistory>();
        }
    }
}