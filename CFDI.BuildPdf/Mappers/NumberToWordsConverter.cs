using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFDI.BuildPdf.Mappers
{
    public static class NumberToWordsConverter
    {
        private static string[] unidades = { "", "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE" };
        private static string[] decenas = { "", "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };
        private static string[] centenas = { "", "CIEN", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS" };

        public static string Convertir(decimal cantidad, string moneda = "MXN")
        {
            int parteEntera = (int)Math.Floor(cantidad);
            int centavos = (int)Math.Round((cantidad - parteEntera) * 100, 0);

            string monedaTexto = GetTextoMoneda(moneda);

            return $"{ConvertirNumero(parteEntera)} {monedaTexto} {centavos:D2}/100 {moneda}";
        }
        private static string ConvertirNumero(int numero)
        {
            if (numero == 0) return "CERO";
            if (numero > 999999) return ConvertirNumero(numero / 1000000) + " MILLONES " + ConvertirNumero(numero % 1000000);
            if (numero > 999) return ConvertirNumero(numero / 1000) + " MIL " + ConvertirNumero(numero % 1000);
            if (numero > 99) return centenas[numero / 100] + " " + ConvertirNumero(numero % 100);
            if (numero > 29) return decenas[numero / 10] + (numero % 10 > 0 ? " Y " + unidades[numero % 10] : "");
            if (numero == 20) return "VEINTE";
            if (numero > 15) return "DIECI" + unidades[numero - 10];
            if (numero == 15) return "QUINCE";
            if (numero == 14) return "CATORCE";
            if (numero == 13) return "TRECE";
            if (numero == 12) return "DOCE";
            if (numero == 11) return "ONCE";
            if (numero == 10) return "DIEZ";
            return unidades[numero];
        }
        private static string GetTextoMoneda(string moneda)
        {
            switch (moneda?.ToUpper())
            {
                case "USD": return "DÓLARES";
                case "EUR": return "EUROS";
                case "MXN":
                default: return "PESOS";
            }
        }
    }

}
