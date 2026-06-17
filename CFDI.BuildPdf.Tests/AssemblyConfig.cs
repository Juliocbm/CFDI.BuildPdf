using Xunit;

// La licencia QuestPDF (QuestPDF.Settings.License) y la fachada estática son estado global de proceso.
// Serializamos los tests para que los que lo manipulan no compitan con los que generan PDFs.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
