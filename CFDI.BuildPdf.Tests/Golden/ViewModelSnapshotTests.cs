using System.Text.Encodings.Web;
using System.Text.Json;
using CFDI.BuildPdf.Mappers.CartaPorte;
using CFDI.BuildPdf.Mappers.Factura;
using CFDI.BuildPdf.Mappers.Nomina;
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

        [Fact]
        [Trait("Category", "Golden")]
        public void Nomina_ViewModel_CoincideConBaseline()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var mapper = new NominaMapper(new FakeQrGenerator());

            var model = mapper.Map(xdoc);
            var json = JsonSerializer.Serialize(model, JsonOpts);

            Snapshot.Match(json, "Nomina.viewmodel.json");
        }

        [Fact]
        [Trait("Category", "Golden")]
        public void CartaPorteRetenciones_ViewModel_CoincideConBaseline()
        {
            var xdoc = TestXmlLoader.LoadCartaPorteRetenciones();
            var mapper = new CartaPorteMapper(new FakeQrGenerator());

            var model = mapper.Map(xdoc);
            var json = JsonSerializer.Serialize(model, JsonOpts);

            Snapshot.Match(json, "CartaPorteRetenciones.viewmodel.json");
        }

        [Fact]
        [Trait("Category", "Golden")]
        public void NominaIncapacidades_ViewModel_CoincideConBaseline()
        {
            var xdoc = TestXmlLoader.LoadNominaIncapacidades();
            var mapper = new NominaMapper(new FakeQrGenerator());

            var model = mapper.Map(xdoc);
            var json = JsonSerializer.Serialize(model, JsonOpts);

            Snapshot.Match(json, "NominaIncapacidades.viewmodel.json");
        }

        [Fact]
        [Trait("Category", "Golden")]
        public void Factura_ViewModel_CoincideConBaseline()
        {
            var xdoc = TestXmlLoader.LoadFacturaIngreso();
            var mapper = new FacturaMapper(new FakeQrGenerator());

            var model = mapper.Map(xdoc);
            var json = JsonSerializer.Serialize(model, JsonOpts);

            Snapshot.Match(json, "Factura.viewmodel.json");
        }

        [Fact]
        [Trait("Category", "Golden")]
        public void FacturaEgreso_ViewModel_CoincideConBaseline()
        {
            var xdoc = TestXmlLoader.LoadFacturaEgreso();
            var mapper = new FacturaMapper(new FakeQrGenerator());

            var model = mapper.Map(xdoc);
            var json = JsonSerializer.Serialize(model, JsonOpts);

            Snapshot.Match(json, "FacturaEgreso.viewmodel.json");
        }
    }
}
