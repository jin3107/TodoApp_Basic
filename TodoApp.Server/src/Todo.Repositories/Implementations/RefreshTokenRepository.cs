using MayNghien.Infrastructures.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Todo.Models.Data;
using Todo.Models.Entities;

namespace Todo.Repositories.Implementations
{
    public class RefreshTokenRepository : GenericRepository<RefreshTokenModel, ApplicationDbContext, ApplicationUser>
    {
        public RefreshTokenRepository(ApplicationDbContext unitOfWork) : base(unitOfWork)
        {
        }
    }
}
