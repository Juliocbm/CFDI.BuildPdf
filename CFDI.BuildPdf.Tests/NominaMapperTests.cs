using CFDI.BuildPdf.Mappers.Nomina;
using CFDI.BuildPdf.Tests.Helpers;

namespace CFDI.BuildPdf.Tests
{
    public class NominaMapperTests
    {
        private readonly NominaMapper _mapper;

        public NominaMapperTests()
        {
            _mapper = new NominaMapper(new FakeQrGenerator());
        }

        [Fact]
        public void Map_DatosComprobante_SonCorrectos()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var model = _mapper.Map(xdoc);

            Assert.Equal("4.0", model.Version);
            Assert.Equal("N", model.Serie);
            Assert.Equal("456", model.Folio);
            Assert.Equal("06600", model.LugarExpedicion);
            Assert.Equal("MXN", model.Moneda);
            Assert.Equal("PUE", model.MetodoPago);
            Assert.Equal("N", model.TipoComprobante);
        }

        [Fact]
        public void Map_Emisor_SonCorrectos()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var model = _mapper.Map(xdoc);

            Assert.Equal("EMPRESA NOMINA SA DE CV", model.EmisorNombre);
            Assert.Equal("ENO030303CCC", model.EmisorRFC);
            Assert.Equal("601", model.EmisorRegimenFiscal);
        }

        [Fact]
        public void Map_Receptor_SonCorrectos()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var model = _mapper.Map(xdoc);

            Assert.Equal("EMPLEADO PRUEBA LOPEZ", model.ReceptorNombre);
            Assert.Equal("EPL040404DDD", model.ReceptorRFC);
            Assert.Equal("CN01", model.UsoCFDI);
        }

        [Fact]
        public void Map_Totales_SonCorrectos()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var model = _mapper.Map(xdoc);

            Assert.Equal(15000.00m, model.SubTotal);
            Assert.Equal(11500.00m, model.Total);
            Assert.Equal(3500.00m, model.Descuento);
        }

        [Fact]
        public void Map_TipoCambio_DefaultUno()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var model = _mapper.Map(xdoc);

            Assert.Equal("1", model.TipoCambio);
        }

        [Fact]
        public void Map_Concepto_ParseadoCorrectamente()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var model = _mapper.Map(xdoc);

            Assert.Single(model.Conceptos);
            var c = model.Conceptos[0];
            Assert.Equal("84111505", c.ClaveProductoServicio);
            Assert.Equal("Pago de nómina", c.Descripcion);
            Assert.Equal(15000.00m, c.ValorUnitario);
            Assert.Equal(3500.00m, c.Descuento);
        }

        [Fact]
        public void Map_SellosYTfd_SonCorrectos()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var model = _mapper.Map(xdoc);

            Assert.Equal("BBB98765-DCBA-4321-HGFE-987654321098", model.UUID);
            Assert.NotNull(model.SelloSAT);
            Assert.NotNull(model.CadenaOriginalSAT);
        }

        [Fact]
        public void Map_QR_Generado()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var model = _mapper.Map(xdoc);

            Assert.NotNull(model.UrlQr);
            Assert.Equal("FAKE_QR_BASE64", model.QRCodeBase64);
        }

        [Fact]
        public void Map_NominaComplemento_DatosGenerales()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var model = _mapper.Map(xdoc);

            Assert.NotNull(model.Nomina);
            Assert.Equal("1.2", model.Nomina.Version);
            Assert.Equal("O", model.Nomina.TipoNomina);
            Assert.Equal(15, model.Nomina.NumDiasPagados);
            Assert.Equal(15000.00m, model.Nomina.TotalPercepciones);
            Assert.Equal(3500.00m, model.Nomina.TotalDeducciones);
            Assert.Equal(0.00m, model.Nomina.TotalOtrosPagos);
        }

        [Fact]
        public void Map_NominaEmisor_RegistroPatronal()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var model = _mapper.Map(xdoc);

            Assert.NotNull(model.Nomina.Emisor);
            Assert.Equal("A1234567890", model.Nomina.Emisor.RegistroPatronal);
        }

        [Fact]
        public void Map_NominaReceptor_DatosEmpleado()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var model = _mapper.Map(xdoc);

            var receptor = model.Nomina.Receptor;
            Assert.NotNull(receptor);
            Assert.Equal("XAXX010101HDFABC01", receptor.Curp);
            Assert.Equal("EMP001", receptor.NumEmpleado);
            Assert.Equal("Sistemas", receptor.Departamento);
            Assert.Equal("Desarrollador", receptor.Puesto);
            Assert.Equal("04", receptor.PeriodicidadPago);
            Assert.Equal(500.00m, receptor.SalarioBaseCotApor);
            Assert.Equal(520.00m, receptor.SalarioDiarioIntegrado);
            Assert.Equal("JAL", receptor.ClaveEntFed);
        }

        [Fact]
        public void Map_Percepciones_ParseadasCorrectamente()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var model = _mapper.Map(xdoc);

            var percepciones = model.Nomina.Percepciones;
            Assert.NotNull(percepciones);
            Assert.Equal(15000.00m, percepciones.TotalSueldos);
            Assert.Equal(12000.00m, percepciones.TotalGravado);
            Assert.Equal(3000.00m, percepciones.TotalExento);
            Assert.Equal(2, percepciones.PercepcionesDetalle.Count);
            Assert.Equal("Sueldo", percepciones.PercepcionesDetalle[0].Concepto);
        }

        [Fact]
        public void Map_Deducciones_ParseadasCorrectamente()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var model = _mapper.Map(xdoc);

            var deducciones = model.Nomina.Deducciones;
            Assert.NotNull(deducciones);
            Assert.Equal(1500.00m, deducciones.TotalOtrasDeducciones);
            Assert.Equal(2000.00m, deducciones.TotalImpuestosRetenidos);
            Assert.Equal(2, deducciones.DeduccionesDetalle.Count);
        }

        [Fact]
        public void Map_OtrosPagos_SubsidioAlEmpleo()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            var model = _mapper.Map(xdoc);

            var otrosPagos = model.Nomina.OtrosPagos;
            Assert.NotNull(otrosPagos);
            Assert.Single(otrosPagos.OtrosPagosDetalle);
            var op = otrosPagos.OtrosPagosDetalle[0];
            Assert.Equal("002", op.TipoOtroPago);
            Assert.NotNull(op.SubsidioAlEmpleo);
            Assert.Equal(407.02m, op.SubsidioAlEmpleo.SubsidioCausado);
        }
    }
}
