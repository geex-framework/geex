using System.Linq;
using System.Threading.Tasks;

using Geex.Common.Abstraction.Auditing;
using Geex.Common.Abstraction.Gql.Types;

using MongoDB.Entities;

using x_Org_x.x_Proj_x.x_Mod_x.Core.Aggregates.x_Aggregate_xs;
using x_Org_x.x_Proj_x.x_Mod_x.Core.GqlSchemas.x_Aggregate_xs.Inputs;

namespace x_Org_x.x_Proj_x.x_Mod_x.Core.GqlSchemas.x_Aggregate_xs
{
    public class x_Aggregate_xMutation : MutationExtension<x_Aggregate_xMutation>, IHasAuditMutation<x_Aggregate_x>
    {
        private readonly DbContext _dbContext;

        public x_Aggregate_xMutation(DbContext dbContext)
        {
            _dbContext = dbContext;
        }
        /// <summary>
        /// 创建x_Aggregate_x
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<x_Aggregate_x> Createx_Aggregate_x(
            Createx_Aggregate_xInput input)
        {
            var entity = new x_Aggregate_x(input.Name);
            return _dbContext.Attach(entity);
        }

        /// <summary>
        /// 编辑x_Aggregate_x
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<bool> Editx_Aggregate_x(string id, Editx_Aggregate_xInput input)
        {
            var entity = _dbContext.Queryable<x_Aggregate_x>().FirstOrDefault(x => x.Id == id);
            if (input.Name != default)
            {
                entity.Name = input.Name;
            }
            return true;
        }

        /// <summary>
        /// 删除x_Aggregate_x
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<bool> Deletex_Aggregate_x(string[] ids)
        {
            await _dbContext.DeleteAsync<x_Aggregate_x>(x => ids.Contains(x.Id));
            return true;
        }
    }
}
