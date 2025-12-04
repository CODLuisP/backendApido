using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VelsatBackendAPI.Data.Repositories
{
    public class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly MySqlConfiguration _configuration;

        public UnitOfWorkFactory(MySqlConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IUnitOfWork Create()
        {
            return new UnitOfWork(_configuration);
        }
    }
}
