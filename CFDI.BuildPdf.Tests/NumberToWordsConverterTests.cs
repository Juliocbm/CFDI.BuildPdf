using CFDI.BuildPdf.Mappers;

namespace CFDI.BuildPdf.Tests
{
    public class NumberToWordsConverterTests
    {
        [Fact]
        public void Convertir_Cero_RetornaCeroPesos()
        {
            var result = NumberToWordsConverter.Convertir(0m, "MXN");
            Assert.Equal("CERO PESOS 00/100 MXN", result);
        }

        [Fact]
        public void Convertir_UnPeso_RetornaUnPesos()
        {
            var result = NumberToWordsConverter.Convertir(1m, "MXN");
            Assert.Equal("UN PESOS 00/100 MXN", result);
        }

        [Fact]
        public void Convertir_ConCentavos_IncluyeCentavos()
        {
            var result = NumberToWordsConverter.Convertir(1500.50m, "MXN");
            Assert.Equal("MIL QUINIENTOS CERO PESOS 50/100 MXN", result);
        }

        [Fact]
        public void Convertir_MonedaUSD_RetornaDolares()
        {
            var result = NumberToWordsConverter.Convertir(100m, "USD");
            Assert.Equal("CIEN DÓLARES 00/100 USD", result);
        }

        [Fact]
        public void Convertir_MonedaEUR_RetornaEuros()
        {
            var result = NumberToWordsConverter.Convertir(250.75m, "EUR");
            Assert.Equal("DOSCIENTOS CINCUENTA EUROS 75/100 EUR", result);
        }

        [Fact]
        public void Convertir_MonedaNull_DefaultPesos()
        {
            var result = NumberToWordsConverter.Convertir(10m, null);
            Assert.Equal("DIEZ PESOS 00/100 ", result);
        }

        [Theory]
        [InlineData(11, "ONCE")]
        [InlineData(12, "DOCE")]
        [InlineData(13, "TRECE")]
        [InlineData(14, "CATORCE")]
        [InlineData(15, "QUINCE")]
        [InlineData(16, "DIECISEIS")]
        [InlineData(21, "VEINTIUN")]
        [InlineData(30, "TREINTA")]
        [InlineData(35, "TREINTA Y CINCO")]
        [InlineData(100, "CIEN")]
        [InlineData(101, "CIEN UN")]
        [InlineData(1000, "MIL")]
        [InlineData(2500, "DOS MIL QUINIENTOS")]
        public void Convertir_NumerosEspeciales_ContieneTextoEsperado(int numero, string textoEsperado)
        {
            var result = NumberToWordsConverter.Convertir(numero, "MXN");
            Assert.Contains(textoEsperado, result);
        }
    }
}
