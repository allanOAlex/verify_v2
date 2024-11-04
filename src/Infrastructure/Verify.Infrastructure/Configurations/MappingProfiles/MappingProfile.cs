using AutoMapper;

using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Logging;
using Verify.Domain.Entities;


namespace Verify.Infrastructure.Configurations.MappingProfiles;
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        #region Account

        CreateMap<Account, AccountInfo>().ReverseMap();

        #endregion

        #region Log

        CreateMap<Log, LogResponse>().ReverseMap();
        CreateMap<Log, CreateLogRequest>().ReverseMap();

        #endregion





    }
}
