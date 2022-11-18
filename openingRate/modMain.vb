Imports System.Threading

Module modMain
    Public moteurEPD As String
    Public livreBIN As String

    Public tabThread() As Thread
    Public nbThreadActif As Integer
    Public nbTaches As Integer
    Public maxThreads As Integer

    Public longueurMaxOuverture As Integer
    Public experienceRate As Integer

    Public moteur_court As String
    Public opening_court As String

    Public tabLOG() As String
    Public fichierLOG As String
    Public tabMessages() As String
    Public tabOuvertures() As String
    Public departEPD() As String
    Public departEPDMem() As String
    Public longueurMaxEPD As Integer

    Public nbOuvertures As Integer
    Public nbOuverturesTraitees As Integer
    Public nbOuverturesTraiteesMem As Integer
    Public dernierIndexOuverture As Integer
    Public indexOuverture As Integer

    Sub Main()
        Dim chaine As String, tabChaine() As String, tabTmp() As String, i As Integer
        Dim fichierINI As String, fichierPGN As String
        Dim modeReverse As Boolean, indexThread As Integer

        Console.Title = My.Computer.Name

        fichierINI = My.Computer.Name & ".ini"
        moteurEPD = "BrainFish.exe"
        livreBIN = "Book.bin"

        If My.Computer.FileSystem.FileExists(fichierINI) Then
            chaine = My.Computer.FileSystem.ReadAllText(fichierINI)
            If chaine <> "" And InStr(chaine, vbCrLf) > 0 Then
                tabChaine = Split(chaine, vbCrLf)
                For i = 0 To UBound(tabChaine)
                    If tabChaine(i) <> "" And InStr(tabChaine(i), " = ") > 0 Then
                        tabTmp = Split(tabChaine(i), " = ")
                        If tabTmp(0) <> "" And tabTmp(1) <> "" Then
                            If InStr(tabTmp(1), "//") > 0 Then
                                tabTmp(1) = Trim(gauche(tabTmp(1), tabTmp(1).IndexOf("//") - 1))
                            End If
                            Select Case tabTmp(0)
                                Case "moteurEPD"
                                    moteurEPD = Replace(tabTmp(1), """", "")

                                Case "livreBIN"
                                    livreBIN = Replace(tabTmp(1), """", "")

                                Case Else

                            End Select
                        End If
                    End If
                Next
            End If
        End If
        My.Computer.FileSystem.WriteAllText(fichierINI, "moteurEPD = " & moteurEPD & vbCrLf _
                                                      & "livreBIN = " & livreBIN & vbCrLf, False)

        moteur_court = nomFichier(moteurEPD)
        maxThreads = cpu()
        nbTaches = maxThreads
        nbThreadActif = 0

        longueurMaxOuverture = 4
        modeReverse = False

        chaine = Replace(Command(), """", "")
        If chaine = "" Then
            Console.WriteLine("Which position ?")
            Console.WriteLine("(enter an UCI string or leave blank for the default startpos)")
            chaine = Trim(Console.ReadLine)

            If Len(Trim(chaine)) > longueurMaxOuverture Then
                longueurMaxOuverture = Len(Trim(chaine))
            End If

            opening_court = "opening"
        End If

        If MsgBox("Normal (Yes) or Reverse (No) ?", MsgBoxStyle.YesNo) = MsgBoxResult.No Then
            modeReverse = True
        End If

        fichierPGN = ""
        fichierLOG = ""

        If My.Computer.FileSystem.FileExists(chaine) Then

            fichierPGN = chaine
            fichierLOG = Replace(nomFichier(fichierPGN), ".pgn", " (" & Replace(nomFichier(livreBIN), ".bin", "") & ").log")

            opening_court = Replace(nomFichier(fichierPGN), ".pgn", "")

            tabOuvertures = listerPositions(fichierPGN)
            nbOuvertures = tabOuvertures.Length - 1

            chaine = InputBox("How many tasks at the same time ?", nbOuvertures & " openings to rate", Format(nbTaches))
            If chaine <> "" Then
                Try
                    nbTaches = CInt(chaine)
                    If nbOuvertures < nbTaches Then
                        nbTaches = nbOuvertures
                    End If
                Catch ex As Exception
                    End
                End Try

                If nbTaches < 1 Then
                    End
                End If
            Else
                End
            End If

        Else

            tabOuvertures = {chaine}
            nbOuvertures = tabOuvertures.Length

            nbTaches = 1

        End If

        ReDim tabLOG(nbOuvertures)
        ReDim tabMessages(nbOuvertures)
        ReDim departEPD(nbOuvertures)
        ReDim departEPDMem(nbOuvertures)

        ReDim tabThread(nbTaches)

        ReDim processus(nbTaches)
        ReDim entree(nbTaches)
        ReDim sortie(nbTaches)

        experienceRate = 0
        nbOuverturesTraitees = 0
        nbOuverturesTraiteesMem = 0
        longueurMaxEPD = 56

        indexThread = 1

        indexOuverture = 1
        While indexOuverture <= nbOuvertures
            'si on est déjà au taquet
            While nbThreadActif >= nbTaches
                'on attend qu'une tache se libère
                Threading.Thread.Sleep(1000)
                affichage()
            End While

            If indexOuverture > nbTaches Then
                While tabThread(indexThread).IsAlive
                    indexThread = indexThread + 1
                    If indexThread > UBound(tabThread) Then
                        indexThread = 1
                    End If
                    Threading.Thread.Sleep(1000)
                End While
            End If

            'exécution
            nbThreadActif = nbThreadActif + 1
            tabThread(indexThread) = New Thread(AddressOf tauxExperience)
            tabThread(indexThread).Start(Format(indexOuverture & ":" & modeReverse & ":" & indexThread))
            indexThread = indexThread + 1
            If indexThread > UBound(tabThread) Then
                indexThread = 1
            End If

            indexOuverture = indexOuverture + 1
        End While

        Do
            Threading.Thread.Sleep(1000)
            affichage()
        Loop While nbThreadActif > 0

        If fichierPGN <> "" Then
            tabLOG(indexOuverture - 1) = vbCrLf & "Opening Rate : " & Format(experienceRate / nbOuverturesTraitees, "0%") & vbCrLf
            My.Computer.FileSystem.WriteAllText(fichierLOG, String.Join(vbCrLf, tabLOG), False)
        End If

        Console.WriteLine("Press ENTER to close the window.")
        Console.ReadLine()
    End Sub

    Private Function listerPositions(fichierPGN As String) As String()
        Dim chaine As String, tabChaine() As String, i As Integer
        Dim fichierUCI As String

        fichierUCI = Replace(fichierPGN, ".pgn", "_uci.pgn")
        If My.Computer.FileSystem.FileExists(fichierUCI) Then
            My.Computer.FileSystem.DeleteFile(fichierUCI)
        End If
        pgnUCI("pgn-extract.exe", fichierPGN, "_uci")
        tabChaine = Split(My.Computer.FileSystem.ReadAllText(fichierUCI), vbCrLf)

        chaine = ""

        For i = 0 To UBound(tabChaine)
            If tabChaine(i) <> "" Then
                If InStr(tabChaine(i), "[") = 0 And InStr(tabChaine(i), "]") = 0 Then
                    tabChaine(i) = Trim(Replace(tabChaine(i), "*", ""))
                    tabChaine(i) = Trim(Replace(tabChaine(i), "1-0", ""))
                    tabChaine(i) = Trim(Replace(tabChaine(i), "0-1", ""))
                    tabChaine(i) = Trim(Replace(tabChaine(i), "1/2-1/2", ""))
                    chaine = chaine & Trim(tabChaine(i)) & vbCrLf
                    If Len(Trim(tabChaine(i))) > longueurMaxOuverture Then
                        longueurMaxOuverture = Len(Trim(tabChaine(i)))
                    End If
                End If
            End If
        Next

        If My.Computer.FileSystem.FileExists(fichierUCI) Then
            My.Computer.FileSystem.DeleteFile(fichierUCI)
        End If

        Return Split(chaine, vbCrLf)
    End Function

    Private Sub tauxExperience(indexes As String)
        Dim chaine As String, tabChaine() As String, tabTmp() As String, i As Integer, positionVide As Boolean
        Dim coup As String, horizon As Integer, positionEPD As String
        Dim tabPositions(10000) As String, tabPosition0Mem As String, indexPosition As Integer, indexOuverture As Integer
        Dim offsetPosition As Integer, nbPositionsMem As Integer, nbCoupsVides As Integer
        Dim position As String, modeReverse As Boolean, indexThread As Integer, poids As String, nbPositions As Integer

        tabChaine = Split(indexes, ":")
        indexOuverture = Int(tabChaine(0))
        position = tabOuvertures(indexOuverture - 1)
        modeReverse = CBool(tabChaine(1))
        indexThread = Int(tabChaine(2))

        departEPDMem(indexOuverture - 1) = ""
        tabPosition0Mem = ""
        nbPositionsMem = 0

        chargerMoteur(moteurEPD, indexThread)

mode_reverse:

        horizon = 80 'nombre de coups maxi

        tabPositions(0) = position
        tabLOG(indexOuverture - 1) = tabPositions(0) & StrDup(longueurMaxOuverture - Len(tabPositions(0)), " ") & " : "
        indexPosition = 0

        departEPD(indexOuverture - 1) = ""

        positionVide = False

        offsetPosition = 0
        nbCoupsVides = 0
        nbPositions = 0

        Do
            position = tabPositions(indexPosition)

            'contrairement à la commande EXPEX, ici on n'obtient que la liste des coups du livre
            'donc on doit utiliser le moteur pour obtenir la chaine EPD pour l'utilitaire pg_query
            positionEPD = positionToEPD(position, indexThread)
            chaine = binbookListe("pg_query.exe", livreBIN, positionEPD)

            If indexPosition = 0 Then
                If tabPositions(0) <> "" Then
                    departEPD(indexOuverture - 1) = positionEPD
                Else
                    departEPD(indexOuverture - 1) = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
                End If
                If Len(departEPD(indexOuverture - 1)) > longueurMaxEPD Then
                    longueurMaxEPD = Len(departEPD(indexOuverture - 1))
                End If
            End If

            If chaine = "" And Not positionVide Then
                chaine = listerCoupsLegaux(position, indexThread)
                positionVide = True
                nbCoupsVides = 0
            End If

            If chaine <> "" Then
                tabChaine = Split(chaine, vbCrLf)
                For i = 0 To UBound(tabChaine)
                    If tabChaine(i) <> "" And InStr(tabChaine(i), "move=", CompareMethod.Text) > 0 Then
                        tabTmp = Split(Replace(tabChaine(i), " ", "="), "=")

                        coup = Trim(tabTmp(1))
                        chaine = Trim(position & " " & coup)

                        poids = Trim(tabTmp(3))
                        If positionVide And poids = "-100%" Then
                            nbCoupsVides = nbCoupsVides + 1
                        End If

                        nbPositions = offsetPosition - nbCoupsVides

                        If nbPositions < 10000 Then '10 000 coups
                            offsetPosition = offsetPosition + 1
                            If offsetPosition > UBound(tabPositions) Then
                                ReDim Preserve tabPositions(offsetPosition * 2)
                            End If
                            tabPositions(offsetPosition) = chaine
                            If nbPositions >= 0 Then
                                tabMessages(indexOuverture - 1) = Trim(Format(nbPositions, "## ##0")) & " moves"
                            End If
                        End If
                    End If

                    'reverse ?
                    If modeReverse Then
                        If nbPositions >= 100 Then
                            indexPosition = indexPosition + 1
                            Exit Do
                        End If
                    End If
                Next
            End If

            indexPosition = indexPosition + 1
        Loop While tabPositions(indexPosition) <> "" And nbCaracteres(tabPositions(indexPosition), " ") < (horizon - 1) And nbPositions < 10000

        'reverse ?
        If modeReverse Then
            If nbPositions < 100 Then
                If Len(tabPositions(0)) >= 9 Then
                    'on sauve la position et les stats actuelles
                    departEPDMem(indexOuverture - 1) = departEPD(indexOuverture - 1)
                    tabPosition0Mem = tabPositions(0)
                    nbPositionsMem = nbPositions

                    position = gauche(tabPositions(0), Len(tabPositions(0)) - 5)
                    Array.Clear(tabPositions, 0, tabPositions.Length)
                    GoTo mode_reverse
                End If
            ElseIf tabPosition0Mem <> "" Then
                'on restaure la position et les stats précédentes
                departEPD(indexOuverture - 1) = departEPDMem(indexOuverture - 1)
                tabPositions(0) = tabPosition0Mem
                nbPositions = nbPositionsMem
            End If
        End If

        chaine = "00 000 moves"
        If nbPositions > 0 Then
            chaine = Format(nbPositions, "00 000") & " moves"
        End If

        nbOuverturesTraitees = nbOuverturesTraitees + 1
        If nbPositions >= 1000 Then
            experienceRate = experienceRate + 1
        End If
        If gauche(chaine, 5) = "00 00" Then
            chaine = Replace(chaine, "00 00", ".....", , 1)
        End If
        If gauche(chaine, 4) = "00 0" Then
            chaine = Replace(chaine, "00 0", "....", , 1)
        End If
        If gauche(chaine, 3) = "00 " Then
            chaine = Replace(chaine, "00 ", "...", , 1)
        End If
        If gauche(chaine, 1) = "0" Then
            chaine = Replace(chaine, "0", ".", , 1)
        End If
        tabMessages(indexOuverture - 1) = Trim(chaine)

        tabLOG(indexOuverture - 1) = tabPositions(0) & StrDup(longueurMaxOuverture - Len(tabPositions(0)), " ") & " : " & chaine

        'décharger le moteur
        dechargerMoteur(indexThread)

        tabPositions = Nothing
        GC.Collect()

        If nbThreadActif > 0 Then
            nbThreadActif = nbThreadActif - 1
        End If

        While Not tabThread(indexThread) Is Nothing
            tabThread(indexThread).Abort()
            Threading.Thread.Sleep(1)
        End While
    End Sub

    Public Sub affichage()
        Dim message As String, nbLignes As Integer, i As Integer

        message = ""
        nbLignes = 0
        For i = nbOuvertures To 1 Step -1
            'd'abord les ouvertures en cours de traitement
            If Not tabMessages(i - 1) Is Nothing And InStr(tabMessages(i - 1), ".") = 0 Then
                If nbLignes < (Console.WindowHeight - 2) Then
                    'message = "Opening " & Format(i, "0000") & " : " & tabMessages(i - 1) & vbCrLf & message
                    message = departEPD(i - 1) & StrDup(longueurMaxEPD - Len(departEPD(i - 1)), " ") & " : " & tabMessages(i - 1) & vbCrLf & message
                    nbLignes = nbLignes + 1
                Else
                    Exit For
                End If
            End If
        Next

        If nbLignes < (Console.WindowHeight - 2) Then
            For i = nbOuvertures To 1 Step -1
                'ensuite les dernières ouvertures traitées s'il y a encore quelques lignes disponibles
                If Not tabMessages(i - 1) Is Nothing And InStr(tabMessages(i - 1), ".") > 0 Then
                    If nbLignes < (Console.WindowHeight - 2) Then
                        'message = "Opening " & Format(i, "0000") & " : " & tabMessages(i - 1) & vbCrLf & message
                        message = departEPD(i - 1) & StrDup(longueurMaxEPD - Len(departEPD(i - 1)), " ") & " : " & tabMessages(i - 1) & vbCrLf & message
                        nbLignes = nbLignes + 1
                    Else
                        Exit For
                    End If
                End If
            Next
        End If

        Console.Clear()
        If nbOuverturesTraitees = 0 Then
            Console.Title = My.Computer.Name & " : openingRate @ 0%, " & opening_court & " @ 0.00% (0)"
        Else
            If nbOuverturesTraitees Mod 10 = 0 And nbOuverturesTraitees > nbOuverturesTraiteesMem Then
                If opening_court <> "opening" Then
                    My.Computer.FileSystem.WriteAllText(fichierLOG, String.Join(vbCrLf, tabLOG), False)
                End If
                nbOuverturesTraiteesMem = nbOuverturesTraitees
            End If
            Console.Title = My.Computer.Name & " : openingRate @ " & Format(experienceRate / nbOuverturesTraitees, "0%") & ", " & opening_court & " @ " & Format(nbOuverturesTraitees / nbOuvertures, "0.00%") & " (" & Trim(Format(nbOuverturesTraitees, "## ##0")) & ")"
        End If
        Console.WriteLine(message)

    End Sub

End Module
