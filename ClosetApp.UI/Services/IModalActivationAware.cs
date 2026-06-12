using System.Threading.Tasks;

namespace ClosetApp.UI.Services;

public interface IModalActivationAware
{
    Task OnModalActivatedAsync();
}
