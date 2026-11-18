Imports System.Threading
Imports ExpertPdf.Anywhere.HtmlToPdf

''' <summary>
''' Runs many conversions on several threads at the same time and measures the throughput, the way a web
''' application or a service converts pages for many users. Run it without arguments for a short test on the
''' bundled sample.html.
''' </summary>
Friend Module Program

    Private s_input As String
    Private s_outputFolder As String
    Private s_conversionsPerThread As Integer
    Private s_completed As Integer
    Private s_failed As Integer
    Private s_clock As Stopwatch

    Function Main(args As String()) As Integer
        If args.Length > 3 OrElse (args.Length > 0 AndAlso (args(0) = "--help" OrElse args(0) = "/?")) Then
            Console.WriteLine("Usage: MultithreadedPerformance [threads] [conversions per thread] [url or html file]")
            Console.WriteLine("  defaults: 4 threads, 3 conversions each, sample.html")
            Return 0
        End If

        Dim appFolder As String = AppDomain.CurrentDomain.BaseDirectory
        Dim threads As Integer = If(args.Length > 0, Integer.Parse(args(0)), 4)
        s_conversionsPerThread = If(args.Length > 1, Integer.Parse(args(1)), 3)

        s_input = If(args.Length > 2, args(2), Path.Combine(appFolder, "sample.html"))
        s_outputFolder = Path.Combine(appFolder, "out")
        Directory.CreateDirectory(s_outputFolder)

        Console.WriteLine("{0} threads x {1} conversions of {2}", threads, s_conversionsPerThread, s_input)
        Console.WriteLine("Output folder: " & s_outputFolder)
        Console.WriteLine()

        s_clock = Stopwatch.StartNew()
        Dim workers(threads - 1) As Thread
        For i As Integer = 0 To threads - 1
            workers(i) = New Thread(AddressOf Worker)
            workers(i).Start(i)
        Next
        For Each worker As Thread In workers
            worker.Join()
        Next

        Dim seconds As Double = s_clock.Elapsed.TotalSeconds
        Console.WriteLine()
        Console.WriteLine("{0} conversions in {1:F1} s: {2:F2} PDF/s. Failed: {3}.", s_completed, seconds, s_completed / seconds, s_failed)
        Return If(s_failed = 0, 0, 1)
    End Function

    Private Sub Worker(state As Object)
        Dim threadIndex As Integer = CInt(state)
        For i As Integer = 0 To s_conversionsPerThread - 1
            Dim outputFile As String = Path.Combine(s_outputFolder, String.Format("thread{0}_conversion{1}.pdf", threadIndex, i))
            Dim watch As Stopwatch = Stopwatch.StartNew()
            Try
                ' PdfConverter is not shared between threads: every conversion has its own instance
                Dim converter As New PdfConverter()
                ' converter.LicenseKey = "your license key"

                If File.Exists(s_input) Then
                    converter.SavePdfFromHtmlFileToFile(s_input, outputFile)
                Else
                    converter.SavePdfFromUrlToFile(s_input, outputFile)
                End If

                Dim completed As Integer = Interlocked.Increment(s_completed)
                Console.WriteLine("Thread {0}, conversion {1}: {2:F2} s. Completed: {3}, average {4:F2} PDF/s",
                    threadIndex, i, watch.Elapsed.TotalSeconds, completed, completed / s_clock.Elapsed.TotalSeconds)
            Catch ex As Exception
                Interlocked.Increment(s_failed)
                Console.WriteLine("Thread {0}, conversion {1} failed: {2}", threadIndex, i, ex.Message)
            End Try
        Next
    End Sub

End Module
