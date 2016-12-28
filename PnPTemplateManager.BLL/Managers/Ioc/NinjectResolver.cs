using Ninject;
using PnPTemplateManager.Managers;
using System.Web.Http.Dependencies;

namespace PnPTemplateManager.BLL.Managers
{
    public class NinjectResolver : NinjectScope, IDependencyResolver
    {
        private IKernel _kernel;
        //public NinjectResolver() : this(new StandardKernel())
        //{
        //}
        public NinjectResolver() : this(Ioc.kernel)
        {
        }
        public NinjectResolver(IKernel kernel)
            : base(kernel)
        {
            _kernel = kernel;
        }
        public IDependencyScope BeginScope()
        {
            return new NinjectScope(_kernel.BeginBlock());
        }
    }
}
