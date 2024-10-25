using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AutoMapper;

using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Log;
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
