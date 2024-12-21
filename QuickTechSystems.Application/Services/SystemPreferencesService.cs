// Path: QuickTechSystems.Application/Services/SystemPreferencesService.cs
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuickTechSystems.Application.DTOs;
using QuickTechSystems.Application.Services.Interfaces;
using QuickTechSystems.Domain.Entities;
using QuickTechSystems.Domain.Interfaces.Repositories;

namespace QuickTechSystems.Application.Services
{
    public class SystemPreferencesService : ISystemPreferencesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public SystemPreferencesService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<SystemPreferenceDTO>> GetUserPreferencesAsync(string userId)
        {
            var preferences = await _unitOfWork.SystemPreferences.Query()
                .Where(p => p.UserId == userId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SystemPreferenceDTO>>(preferences);
        }

        public async Task<string> GetPreferenceValueAsync(string userId, string key, string defaultValue = "")
        {
            var preference = await _unitOfWork.SystemPreferences.Query()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.PreferenceKey == key);

            return preference?.PreferenceValue ?? defaultValue;
        }

        public async Task SavePreferenceAsync(string userId, string key, string value)
        {
            var preference = await _unitOfWork.SystemPreferences.Query()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.PreferenceKey == key);

            if (preference == null)
            {
                preference = new SystemPreference
                {
                    UserId = userId,
                    PreferenceKey = key,
                    PreferenceValue = value,
                    LastModified = DateTime.Now
                };
                await _unitOfWork.SystemPreferences.AddAsync(preference);
            }
            else
            {
                preference.PreferenceValue = value;
                preference.LastModified = DateTime.Now;
                await _unitOfWork.SystemPreferences.UpdateAsync(preference);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task SavePreferencesAsync(string userId, Dictionary<string, string> preferences)
        {
            foreach (var (key, value) in preferences)
            {
                await SavePreferenceAsync(userId, key, value);
            }
        }

        public async Task InitializeUserPreferencesAsync(string userId)
        {
            var defaultPreferences = new Dictionary<string, string>
            {
                { "Theme", "Light" },
                { "Language", "en-US" },
                { "TimeZone", "UTC" },
                { "DateFormat", "MM/dd/yyyy" },
                { "TimeFormat", "HH:mm:ss" },
                { "ReceiptPrinter", "Default" },
                { "BarcodeScannerEnabled", "true" },
                { "ShowGridLines", "true" },
                { "ItemsPerPage", "20" },
                { "AutoLogoutMinutes", "30" },
                { "EnableNotifications", "true" },
                { "SoundEffects", "true" }
            };

            await SavePreferencesAsync(userId, defaultPreferences);
        }
    }
}