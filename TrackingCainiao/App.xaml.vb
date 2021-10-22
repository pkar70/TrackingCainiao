Imports Windows.ApplicationModel.Background
''' <summary>
''' Provides application-specific behavior to supplement the default Application class.
''' </summary>
NotInheritable Class App
    Inherits Application

    Shared rootFrame As Frame

    Protected Function OnLaunchFragment(aes As ApplicationExecutionState) As Frame
        Dim mRootFrame As Frame = TryCast(Window.Current.Content, Frame)

        ' Do not repeat app initialization when the Window already has content,
        ' just ensure that the window is active

        If mRootFrame Is Nothing Then
            ' Create a Frame to act as the navigation context and navigate to the first page
            mRootFrame = New Frame()

            AddHandler mRootFrame.NavigationFailed, AddressOf OnNavigationFailed

            ' PKAR added wedle https://stackoverflow.com/questions/39262926/uwp-hardware-back-press-work-correctly-in-mobile-but-error-with-pc
            AddHandler mRootFrame.Navigated, AddressOf OnNavigatedAddBackButton
            AddHandler Windows.UI.Core.SystemNavigationManager.GetForCurrentView().BackRequested, AddressOf OnBackButtonPressed

            ' Place the frame in the current Window
            Window.Current.Content = mRootFrame
        End If

        Return mRootFrame
    End Function

    'Protected Async Function OnLaunchFragment(aes As ApplicationExecutionState) As Task
    '    rootFrame = TryCast(Window.Current.Content, Frame)

    '    ' Do not repeat app initialization when the Window already has content,
    '    ' just ensure that the window is active

    '    If rootFrame Is Nothing Then
    '        ' Create a Frame to act as the navigation context and navigate to the first page
    '        rootFrame = New Frame()

    '        AddHandler rootFrame.NavigationFailed, AddressOf OnNavigationFailed

    '        If aes = ApplicationExecutionState.Terminated Then
    '            Await LoadItemsAsync()
    '            ' TODO: Load state from previously suspended application
    '        End If
    '        ' Place the frame in the current Window
    '        Window.Current.Content = rootFrame
    '    End If

    'End Function

    ''' <summary>
    ''' Invoked when the application is launched normally by the end user.  Other entry points
    ''' will be used when the application is launched to open a specific file, to display
    ''' search results, and so forth.
    ''' </summary>
    ''' <param name="e">Details about the launch request and process.</param>
    Protected Overrides Async Sub OnLaunched(e As Windows.ApplicationModel.Activation.LaunchActivatedEventArgs)

        Dim RootFrame As Frame = OnLaunchFragment(e.PreviousExecutionState)

        If e.PreviousExecutionState = ApplicationExecutionState.Terminated Then
            Await LoadItemsAsync()
            ' TODO: Load state from previously suspended application
        End If


        If e.PrelaunchActivated = False Then
            If rootFrame.Content Is Nothing Then
                ' When the navigation stack isn't restored navigate to the first page,
                ' configuring the new page by passing required information as a navigation
                ' parameter
                rootFrame.Navigate(GetType(MainPage), e.Arguments)
            End If

            ' Ensure the current window is active
            Window.Current.Activate()
        End If
    End Sub

    ''' <summary>
    ''' Invoked when Navigation to a certain page fails
    ''' </summary>
    ''' <param name="sender">The Frame which failed navigation</param>
    ''' <param name="e">Details about the navigation failure</param>
    Private Sub OnNavigationFailed(sender As Object, e As NavigationFailedEventArgs)
        Throw New Exception("Failed to load Page " + e.SourcePageType.FullName)
    End Sub

    ''' <summary>
    ''' Invoked when application execution is being suspended.  Application state is saved
    ''' without knowing whether the application will be terminated or resumed with the contents
    ''' of memory still intact.
    ''' </summary>
    ''' <param name="sender">The source of the suspend request.</param>
    ''' <param name="e">Details about the suspend request.</param>
    Private Sub OnSuspending(sender As Object, e As SuspendingEventArgs) Handles Me.Suspending
        Dim deferral As SuspendingDeferral = e.SuspendingOperation.GetDeferral()
        ' TODO: Save application state and stop any background activity
        deferral.Complete()
    End Sub

    Public Shared gItems As ObservableCollection(Of JednaPaczka)

    Public Shared Async Function SaveItemsAsync() As Task(Of Boolean)
        If App.gItems.Count < 1 Then Return False

        Dim oFold As Windows.Storage.StorageFolder = Windows.Storage.ApplicationData.Current.LocalFolder
        Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(App.gItems, Newtonsoft.Json.Formatting.Indented)

        Await oFold.WriteAllTextToFileAsync("items.json", sTxt, Windows.Storage.CreationCollisionOption.ReplaceExisting)

        Return True
    End Function

    Public Shared Async Function LoadItemsAsync() As Task(Of Boolean)

        Dim sTxt As String = Await Windows.Storage.ApplicationData.Current.LocalFolder.ReadAllTextFromFileAsync("items.json")
        If sTxt Is Nothing OrElse sTxt.Length < 5 Then
            App.gItems = New ObservableCollection(Of JednaPaczka)
            Return False
        End If
        App.gItems = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(ObservableCollection(Of JednaPaczka)))
        Return True
    End Function

    Public Shared Function GetSyLink(oItem As JednaPaczka) As String
        Return "https://www.sypost.net/queryTrack?queryTime=" & DateTimeOffset.Now.ToUnixTimeMilliseconds.ToString &
            "-12293&toLanguage=en_US&trackNumber=" & oItem.sNumer
    End Function

    Private Shared Async Function GetWebDataSYpostSingle(bMsg As Boolean, oItem As JednaPaczka) As Task(Of Integer)

        ' var timestamp = Date.now() + "-" + Math.round(Math.random() * 9999 + 10000);
        ' script.src = "/queryTrack?queryTime=" + timestamp + "&toLanguage=" + toLanguage + "&trackNumber=" + _trackNumber;

        ' Windows.Security.Cryptography.CryptographicBuffer.GenerateRandomNumber()

        Dim sUri As String = GetSyLink(oItem)

        Dim sPage As String = Await HttpPageAsync(sUri, "Error getting page", bMsg)
        If sPage Is Nothing Then
            DialogOrToast(bMsg, "GetWebDataSYpostSingle HttpPageAsync returns null")
            Return -1
        End If

        ' searchCallback({"data":[{"result":{"origin":{"items":[{"content":"Pre Alert to Poland","createTime":1605186751000,"status":101,"timeZone":"+8"},{"content":"CHINA, SHENZHEN, Acceptance, Sent to Poland","createTime":1605871782000,"status":102,"timeZone":"+8"},{"content":"CHINA, SHENZHEN, Departed Sunyou Facility","createTime":1606059396000,"status":105,"timeZone":"+8"},{"content":"HONG KONG, Departed HONG KONG Airport","createTime":1606115517000,"status":106,"timeZone":"+8"}]}},"orderNo":"SYAET02761875","lastContent":"HONG KONG, Departed HONG KONG Airport","lastUpdate":1606115517000,"dstCountry":"PL","days":6,"displayStatus":1,"has":true,"lastStatus":106}],"message":"ok","status":1})
        ' przycięcie - tylko JSON nas interesuje
        Dim iInd As Integer = sPage.IndexOf("{&quot;data&quot;")
        If iInd < 10 Then
            DialogOrToast(bMsg, "ERROR GetWebDataSYpostSingle jakoby nie było początku JSON dla " & oItem.sNazwa)
            Return -1
        End If
        sPage = sPage.Substring(iInd)

        ' ostatni nawias, czyli zamknięcie parametrów searchCallBack
        sPage = sPage.Substring(0, sPage.Length - 1)

        Dim oJsonWebResponse As JsonWebResponse = Nothing
        Try
            oJsonWebResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(JsonSyWebResponse))
        Catch ex As Exception

        End Try
        If oJsonWebResponse Is Nothing Then
            DialogOrToast(bMsg, "ERROR GetWebDataSYpostSingle converting JSON from web")
            Return -1
        End If

        ' oItem do zmiany?

        Return 1    ' 1 zmiana

    End Function

    Private Shared Async Function GetWebDataSYpost(bMsg As Boolean) As Task(Of Integer)

        If gItems Is Nothing Then Return -1 ' to sie nie powinno zdarzyc!
        Dim iCnt As Integer = 0

        For Each oItem As JednaPaczka In gItems
            If oItem.bDostalem Then Continue For
            If Not oItem.sNumer.StartsWith("SY") Then Continue For ' nie SY robimy via Cainaio

            Dim iRet As Integer = Await GetWebDataSYpostSingle(bMsg, oItem)
            If iRet = -1 Then Exit For

            If iRet > 0 Then iCnt += 1

        Next

        Return iCnt
    End Function


    Public Shared Async Function GetWebData(bMsg As Boolean) As Task(Of Integer)

        If gItems Is Nothing Then
            If Not bMsg Then MakeToast("GetWebData ale gItems is null")
            Return -1
        End If

        ' If Not bMsg Then MakeToast("GetWebData starting " & DateTime.Now.ToString("yyyy-MM-dd HH:mm"))

        Dim iRet1 As Integer = Await GetWebDataCainaio(bMsg)
        Dim iRet2 As Integer = 0 ' Await GetWebDataSYpost(bMsg)

        If iRet1 = -1 Then Return iRet2
        If iRet2 = -1 Then Return iRet1

        Return iRet1 + iRet2

    End Function


    Private Shared Async Function GetWebDataCainaio(bMsg As Boolean) As Task(Of Integer)

        If gItems Is Nothing Then Return -1 ' to sie nie powinno zdarzyc!

        Dim sUri As String = "https://global.cainiao.com/detail.htm?mailNoList="
        Dim bFirst As Boolean = True

        ' doklejenie tracking#, tylko tego co jeszcze nie jest otrzymane
        For Each oItem As JednaPaczka In gItems
            If oItem.bDostalem Then Continue For
            'If oItem.sNumer.StartsWith("SY") Then Continue For ' dla SY robimy niezalezne sprawdzenie gdzie indziej

            If Not bFirst Then sUri &= ","
            sUri &= oItem.sNumer
            bFirst = False
        Next

        Dim sPage As String = Await HttpPageAsync(sUri, "Error getting page", bMsg)
        If sPage Is Nothing Then
            If Not bMsg Then MakeToast("GetWebData HttpPageAsync returns null")
            Return -1
        End If

        ' przycięcie - tylko JSON nas interesuje
        Dim iInd As Integer = sPage.IndexOf("{&quot;data&quot;")
        If iInd < 10 Then
            DialogOrToast(bMsg, "ERROR GetWebData jakoby nie było początku JSON")
            Return -1
        End If
        sPage = sPage.Substring(iInd)
        iInd = sPage.IndexOf("</textarea>")
        If iInd < 10 Then
            DialogOrToast(bMsg, "ERROR GetWebData jakoby nie było końca JSON")
            Return -1
        End If
        sPage = sPage.Substring(0, iInd)

        ' podmiana &quot;
        sPage = sPage.Replace("&quot;", """")


        Dim oJsonWebResponse As JsonWebResponse = Nothing
        Try
            oJsonWebResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(sPage, GetType(JsonWebResponse))
        Catch ex As Exception

        End Try
        If oJsonWebResponse Is Nothing Then
            DialogOrToast(bMsg, "ERROR GetWebData converting JSON from web")
            Return -1
        End If

        Dim iChanged As Integer = 0

        'aktualizacja  App.gItems według oJsonWebResponse
        For Each oItemJson As JsonOneShipment In oJsonWebResponse.data
            Debug.WriteLine("Mam dane dla Shipment: " & oItemJson.mailNo)

            For Each oItemLocal As JednaPaczka In App.gItems

                If oItemLocal.sNumer = oItemJson.mailNo Then
                    Debug.WriteLine("i jest to " & oItemLocal.sNazwa)

                    If oItemLocal.bDostalem Then Exit For ' jeśli już otrzymana, to ignorujemy to - bo nasz wpis jest ostatni

                    oItemLocal.JSONshipment = oItemJson

                    ' Cainaio wyciąga chyba nie to co trzeba (tzn. nie ostatni naprawdę, tylko ostatni "większy" 

                    Dim oJsonDetail As JsonOneDetail

                    Dim oJsonList As List(Of JsonOneDetail) = oItemJson?.section2?.detailList
                    If oJsonList IsNot Nothing AndAlso oJsonList.Count > 0 Then
                        oJsonDetail = oJsonList.ElementAt(0)
                    Else
                        oJsonDetail = oItemJson?.latestTrackingInfo
                    End If

                    If oJsonDetail Is Nothing Then
                        Debug.WriteLine("ale nie posiadam żadnych info o losie przesyłki!")
                    Else

                        Dim sNowaData As String = KonwersjaDaty(oJsonDetail.time, oJsonDetail.timeZone)
                        If sNowaData <> oItemLocal.sLastDate Then
                            iChanged += 1
                            Dim sMsg As String = oItemLocal.sNazwa & ": " & oJsonDetail.desc
                            If bMsg Then
                                Await DialogBoxAsync(sMsg)
                            Else
                                ' jesteśmy bez Userinterface, pewnikiem na Timer
                                MakeToast(sMsg)
                            End If
                            oItemLocal.sLastEvent = oJsonDetail.desc
                            oItemLocal.sLastDate = sNowaData
                            oItemLocal.JSONshipment.section2.detailList = oItemJson?.section2?.detailList
                        End If
                    End If

                    Exit For
                End If
            Next

        Next

        'If Not bMsg Then MakeToast("GetWebData po wczytaniu") 

        Return iChanged

    End Function

    Public Shared Async Function GetPPWebData(bMsg As Boolean) As Task(Of Integer)
        ' PocztaPolska
        If gItems Is Nothing Then Return -1

        ' doklejenie tracking#, tylko tego co jeszcze nie jest otrzymane
        For Each oItem As JednaPaczka In gItems
            If oItem.bDostalem Then Continue For

            ' If oItem.sNumer <> "00459007738840781764" Then Continue For
            If oItem.sNumer <> "TEGONAPEWNONIEMA" Then Continue For


            Dim sUri As String = "https://emonitoring.poczta-polska.pl/?numer=" & oItem.sNumer
            Dim sData As String = "" '= "ue&l=&n=" & oItem.sNumer
            Dim sPage As String = Await HttpPageAsync(sUri, "Error getting page", bMsg)
            Await Threading.Tasks.Task.Delay(100)
            ' https://emonitoring.poczta-polska.pl/wssClient.php?n=00459007738840781764
            If sPage Is Nothing Then
                If Not bMsg Then MakeToast("GetPPWebData HttpPageAsync returns null")
                Continue For
            End If

            'If sPage.Contains("Przepraszamy, aktualnie obsługujemy bardzo dużą ilość zapytań") Then
            '    Debug.WriteLine("Za dużo zapytań")
            '    Continue For
            'End If

            ' "<script src="" js/script.js"" type=""text/javascript""></script>" & vbLf & "Podaj numer przesyłki                    <script>" & vbLf & "        jQuery('#BSzukajO').click(function (e) {" & vbLf & vbLf & "            e.preventDefault();" & vbLf & vbLf & "            //$.post('wssClient.php', {n:$('#numer').val() }, function(data){$('#wyniki').html(data);});" & vbLf & "            if (jQuery('#numer').val().length == 19) {" & vbLf & "                var numer = dodajck(jQuery('#numer').val())" & vbLf & "            } else" & vbLf & "                var numer = jQuery('#numer').val();" & vbLf & vbLf & "            jQuery.ajax({" & vbLf & "                type: 'post'," & vbLf & "                cache: true," & vbLf & "                dataType: 'html'," & vbLf & "                data: ({arch: 'true', s: 'pu8vur0bff96crgjeo47pned93', n: numer, l: ''})," & vbLf & "                url: 'wssClient.php'," & vbLf & "                error: function (xhr, ajaxOptions, thrownError) {" & vbLf & "                    jQuery('#wyniki').html('<h2>Przepraszamy, aktualnie obsługujemy bardzo dużą ilość zapytań. Prosimy spróbować ponownie za kilka minut. (4)</h2>')" & vbLf & "                }," & vbLf & "                beforeSend: function () {" & vbLf & "                    jQuery('#wyniki').html('<img style="" margin-left:270px;margin-top:90px"" id=""indic"" src=""css/ajax-loader.gif"" />')" & vbLf & "                }," & vbLf & "                success: function (data) {" & vbLf & "                    jQuery('#wyniki').html(data);" & vbLf & "                }," & vbLf & "                complete: function () {" & vbLf & "                    jQuery('#indic').remove()" & vbLf & "                }" & vbLf & "            });" & vbLf & vbLf & "            return false;" & vbLf & "        });" & vbLf & "    </script>"

            Dim iInd As Integer

            ' w stronie: "zadarzenia"
            If Not sPage.Contains("darzenia_td") Then

                iInd = sPage.IndexOf("s: '")
                If iInd < 0 Then
                    Debug.WriteLine("Nie mam tabelki zdarzeń ni parametru s")
                    Continue For
                End If

                sPage = sPage.Substring(iInd + 4)
                iInd = sPage.IndexOf("'")
                If iInd < 0 OrElse iInd > 35 Then
                    Debug.WriteLine("błąd parametru s")
                    Continue For
                End If

                sData = sData & "&s=" & sPage.Substring(0, iInd)
                sPage = Await HttpPageAsync(sUri, "Error getting page", bMsg, sData)
                Await Threading.Tasks.Task.Delay(100)

                If sPage Is Nothing Then
                    If Not bMsg Then MakeToast("GetPPWebData HttpPageAsync returns null - DRUGI")
                    Continue For
                End If

                If sPage.Contains("Przepraszamy, aktualnie obsługujemy bardzo dużą ilość zapytań") Then
                    Debug.WriteLine("Za dużo zapytań - DRUGI")
                    Continue For
                End If


            End If

            iInd = sPage.IndexOf("darzenia_td")  ' w stronie: "zadarzenia"

            sPage = sPage.Substring(iInd + 10)
            iInd = sPage.IndexOf("</table")
            sPage = sPage.Substring(0, iInd)


        Next

        Return 1

    End Function


    Public Async Function AppServiceLocalCommand(sCommand As String) As Task(Of String)

    End Function

    Public Shared Function KonwersjaDaty(sData As String, sTimeZone As String) As String
        ' **TODO** przesunięcie
        '"2020-10-21 19:57:58"
        '"+2"

        Dim sFormat As String = "yyyy-MM-dd HH:mm:ss"
        Dim oProvider As IFormatProvider = Globalization.CultureInfo.InvariantCulture.DateTimeFormat

        Dim oDate As DateTime
        If Not DateTime.TryParseExact(sData, sFormat, oProvider, System.Globalization.DateTimeStyles.AssumeLocal, oDate) Then Return sData

        Dim dOffset As Double
        If Not Double.TryParse(sTimeZone, dOffset) Then Return sData

        oDate = oDate.AddHours(dOffset)

        Return oDate.ToString(sFormat)
    End Function

#Region "Triggers set/reset"
    ' CommandLine, Toasts
    Protected Overrides Async Sub OnActivated(args As IActivatedEventArgs)

        If args.Kind = ActivationKind.CommandLineLaunch Then

            Dim commandLine As CommandLineActivatedEventArgs = TryCast(args, CommandLineActivatedEventArgs)
            Dim operation As CommandLineActivationOperation = commandLine?.Operation
            Dim strArgs As String = operation?.Arguments

            If Not String.IsNullOrEmpty(strArgs) Then
                Await ObsluzCommandLine(strArgs)
                Window.Current.Close()
                Return
            End If
        End If

        ' jesli nie cmdline (a np. toast), albo cmdline bez parametrow, to pokazujemy okno
        Dim rootFrame As Frame = OnLaunchFragment(args.PreviousExecutionState)

        If rootFrame.Content Is Nothing Then
            rootFrame.Navigate(GetType(MainPage))
        End If

        Window.Current.Activate()
    End Sub


    Protected Overrides Async Sub OnBackgroundActivated(args As BackgroundActivatedEventArgs)
        Dim oTimerDeferal As BackgroundTaskDeferral
        oTimerDeferal = args.TaskInstance.GetDeferral()

        If App.gItems Is Nothing Then
            Await LoadItemsAsync()
        End If

        Select Case args.TaskInstance.Task.Name
            Case "SledzikTimer"
                Dim iChanged As Integer = Await GetWebData(False)
                'If Await GetPPWebData(False) Then iChanged = True
                If iChanged > 0 Then Await SaveItemsAsync()
        End Select

        'If rootFrame IsNot Nothing Then
        '    MakeToast("onbackground rootframe", "onbackground rootframe", "onbackground rootframe")
        '    Dim oMain As MainPage = TryCast(rootFrame.Content, MainPage)
        '    If oMain IsNot Nothing Then Await oMain.CalledFromBackground(args.TaskInstance)
        'Else
        '    MakeToast("onbackground NULL", "onbackground NULL", "onbackground NULL")
        'End If

        oTimerDeferal.Complete()
    End Sub

#End Region

End Class
