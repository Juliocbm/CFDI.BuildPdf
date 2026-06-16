using System.Text.Encodings.Web;
using System.Text.Json;
using CFDI.BuildPdf.Mappers.CartaPorte;
using CFDI.BuildPdf.Tests.Helpers;
using Xunit;

namespace CFDI.BuildPdf.Tests.Golden
{
    public class ViewModelSnapshotTests
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            // System.Text.Json codifica '&' como & por defecto; lo fijamos para que el baseline no dependa de cambios futuros del encoder.
            Encoder = JavaScriptEncoder.Default
        };

        [Fact]
        [Trait("Category", "Golden")]
        public void CartaPorte_ViewModel_CoincideConBaseline()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var mapper = new CartaPorteMapper(new FakeQrGenerator());

            var model = mapper.Map(xdoc);
            var json = JsonSerializer.Serialize(model, JsonOpts);

            Snapshot.Match(json, "CartaPorte.viewmodel.json");
        }
    }
}
