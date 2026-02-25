using CFDI.BuildPdf.Mappers.CartaPorte;
using CFDI.BuildPdf.Tests.Helpers;

namespace CFDI.BuildPdf.Tests
{
    public class CartaPorteMapperTests
    {
        private readonly CartaPorteMapper _mapper;

        public CartaPorteMapperTests()
        {
            _mapper = new CartaPorteMapper(new FakeQrGenerator());
        }

        [Fact]
        public void CanMap_CartaPorteXml_RetornaTrue()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            Assert.True(_mapper.CanMap(xdoc));
        }

        [Fact]
        public void CanMap_NominaXml_RetornaFalse()
        {
            var xdoc = TestXmlLoader.LoadNomina();
            Assert.False(_mapper.CanMap(xdoc));
        }

        [Fact]
        public void Map_DatosComprobante_SonCorrectos()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.Equal("4.0", model.Version);
            Assert.Equal("A", model.Serie);
            Assert.Equal("123", model.Folio);
            Assert.Equal("06600", model.LugarExpedicion);
            Assert.Equal("MXN", model.Moneda);
            Assert.Equal("PPD", model.MetodoPago);
            Assert.Equal("T", model.TipoComprobante);
            Assert.Equal("01", model.Exportacion);
            Assert.Equal("CONTADO", model.CondicionesPago);
        }

        [Fact]
        public void Map_Emisor_SonCorrectos()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.Equal("EMPRESA DEMO SA DE CV", model.EmisorNombre);
            Assert.Equal("EDE010101AAA", model.EmisorRFC);
            Assert.Equal("601", model.EmisorRegimenFiscal);
        }

        [Fact]
        public void Map_Receptor_SonCorrectos()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.Equal("CLIENTE PRUEBA SA DE CV", model.ReceptorNombre);
            Assert.Equal("CPR020202BBB", model.ReceptorRFC);
            Assert.Equal("44100", model.ReceptorDomicilioFiscal);
            Assert.Equal("601", model.ReceptorRegimenFiscal);
            Assert.Equal("S01", model.UsoCFDI);
        }

        [Fact]
        public void Map_Conceptos_ContieneUnConcepto()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.Single(model.Conceptos);
            var concepto = model.Conceptos[0];
            Assert.Equal("78101800", concepto.ClaveProductoServicio);
            Assert.Equal("Servicio de transporte", concepto.Descripcion);
            Assert.Equal(1.00m, concepto.Cantidad);
        }

        [Fact]
        public void Map_Conceptos_TrasladosParseados()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            var traslados = model.Conceptos[0].Traslados;
            Assert.Single(traslados);
            Assert.Equal("002", traslados[0].Impuesto);
            Assert.Equal(0.160000m, traslados[0].TasaOCuota);
            Assert.Equal(16.00m, traslados[0].Importe);
        }

        [Fact]
        public void Map_TotalImpuestosTrasladados_EsCorrecto()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.Equal(16.00m, model.TotalImpuestosTrasladados);
        }

        [Fact]
        public void Map_SellosYTfd_SonCorrectos()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.Equal("AAB12345-ABCD-1234-EFGH-123456789012", model.UUID);
            Assert.Equal("00001000000504465028", model.NoCertificadoSAT);
            Assert.Equal("SelloSATDePrueba12345678", model.SelloSAT);
            Assert.NotNull(model.CadenaOriginalSAT);
            Assert.StartsWith("||1.1|", model.CadenaOriginalSAT);
        }

        [Fact]
        public void Map_QR_Generado()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.NotNull(model.UrlQr);
            Assert.Contains("AAB12345-ABCD-1234-EFGH-123456789012", model.UrlQr);
            Assert.Equal("FAKE_QR_BASE64", model.QRCodeBase64);
        }

        [Fact]
        public void Map_CantidadConLetra_NoEsNull()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.NotNull(model.CantidadConLetra);
            Assert.Contains("MXN", model.CantidadConLetra);
        }

        [Fact]
        public void Map_CartaPorteComplemento_DatosGenerales()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.NotNull(model.CartaPorte);
            Assert.Equal("3.1", model.CartaPorte.Version);
            Assert.Equal("No", model.CartaPorte.TransporteInternacional);
            Assert.Equal(150.50m, model.CartaPorte.DistanciaRecorrida);
            Assert.Equal("CCC-00001", model.CartaPorte.IdCCP);
            Assert.Equal(500.00m, model.CartaPorte.PesoBrutoTotal);
            Assert.Equal("KGM", model.CartaPorte.UnidadPeso);
            Assert.Equal(2, model.CartaPorte.NumeroTotalMercancias);
        }

        [Fact]
        public void Map_CartaPorte_Ubicaciones()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.Equal(2, model.CartaPorte.Ubicaciones.Count);

            var origen = model.CartaPorte.Ubicaciones[0];
            Assert.Equal("Origen", origen.TipoUbicacion);
            Assert.Equal("06600", origen.CodigoPostal);
            Assert.Equal("CMX", origen.Estado);

            var destino = model.CartaPorte.Ubicaciones[1];
            Assert.Equal("Destino", destino.TipoUbicacion);
            Assert.Equal("44100", destino.CodigoPostal);
            Assert.Equal("JAL", destino.Estado);
        }

        [Fact]
        public void Map_CartaPorte_Mercancias()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.Equal(2, model.CartaPorte.MercanciasDetalle.Count);
            Assert.Equal("Producto A", model.CartaPorte.MercanciasDetalle[0].Descripcion);
            Assert.Equal(250.00m, model.CartaPorte.MercanciasDetalle[0].PesoEnKg);
            Assert.Equal("Producto B", model.CartaPorte.MercanciasDetalle[1].Descripcion);
        }

        [Fact]
        public void Map_CartaPorte_Autotransporte()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.NotNull(model.CartaPorte.Autotransporte);
            Assert.Equal("TPAF01", model.CartaPorte.Autotransporte.PermisoSCT);
            Assert.Equal("C2", model.CartaPorte.Autotransporte.ConfigVehicular);
            Assert.Equal("ABC1234", model.CartaPorte.Autotransporte.PlacaVM);
            Assert.Equal(2022, model.CartaPorte.Autotransporte.AnioModeloVM);
        }

        [Fact]
        public void Map_CartaPorte_Seguros()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.NotNull(model.CartaPorte.Seguro);
            Assert.Equal("Seguros Ejemplo", model.CartaPorte.Seguro.AseguradoraResponsabilidadCivil);
            Assert.Equal("POL-RC-001", model.CartaPorte.Seguro.PolizaResponsabilidadCivil);
        }

        [Fact]
        public void Map_CartaPorte_Remolque()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.NotNull(model.CartaPorte.Remolque);
            Assert.Equal("CTR001", model.CartaPorte.Remolque.SubTipoRemolque);
            Assert.Equal("REM9876", model.CartaPorte.Remolque.Placa);
        }

        [Fact]
        public void Map_CartaPorte_FigurasTransporte()
        {
            var xdoc = TestXmlLoader.LoadCartaPorte();
            var model = _mapper.Map(xdoc);

            Assert.Single(model.CartaPorte.FigurasTransporte);
            var figura = model.CartaPorte.FigurasTransporte[0];
            Assert.Equal("01", figura.TipoFigura);
            Assert.Equal("JUAN PEREZ", figura.NombreFigura);
            Assert.Equal("LIC123456", figura.NumeroLicencia);
        }
    }
}
