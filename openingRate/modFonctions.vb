Imports System.Management 'ajouter référence system.management
Imports VB = Microsoft.VisualBasic

Module modFonctions
    Public processus() As System.Diagnostics.Process
    Public entree() As System.IO.StreamWriter
    Public sortie() As System.IO.StreamReader

    Public Function binbookListe(pg_query As String, book As String, epd As String) As String
        Dim chaine As String

        chaine = ""

        If My.Computer.FileSystem.FileExists(book) Then
            Dim processusQuery As New System.Diagnostics.Process()
            processusQuery.StartInfo.RedirectStandardOutput = True
            processusQuery.StartInfo.UseShellExecute = False
            processusQuery.StartInfo.CreateNoWindow = True
            processusQuery.StartInfo.FileName = ("""" & pg_query & """")
            processusQuery.StartInfo.Arguments = ("""" & book & """" & " " & """" & epd & """")
            processusQuery.Start()
            chaine = processusQuery.StandardOutput.ReadToEnd
            processusQuery.Close()
            processusQuery = Nothing
            GC.Collect()
        End If

        Return chaine
    End Function

    Public Sub chargerMoteur(chemin As String, indexThread As Integer)
        Dim chaine As String

chargement_moteur:
        Try
            processus(indexThread) = New System.Diagnostics.Process()

            processus(indexThread).StartInfo.RedirectStandardOutput = True
            processus(indexThread).StartInfo.UseShellExecute = False
            processus(indexThread).StartInfo.RedirectStandardInput = True
            processus(indexThread).StartInfo.CreateNoWindow = True
            processus(indexThread).StartInfo.WorkingDirectory = My.Application.Info.DirectoryPath
            processus(indexThread).StartInfo.FileName = chemin
            processus(indexThread).Start()
            processus(indexThread).PriorityClass = 64 '64 (idle), 16384 (below normal), 32 (normal), 32768 (above normal), 128 (high), 256 (realtime)

            entree(indexThread) = processus(indexThread).StandardInput
            sortie(indexThread) = processus(indexThread).StandardOutput

            entree(indexThread).WriteLine("uci")
            chaine = ""
            While InStr(chaine, "uciok") = 0
                If processus(indexThread).HasExited Then
                    entree(indexThread).Close()
                    sortie(indexThread).Close()
                    processus(indexThread).Close()
                    GoTo chargement_moteur
                End If
                chaine = sortie(indexThread).ReadLine
                Threading.Thread.Sleep(10)
            End While

            entree(indexThread).WriteLine("setoption name threads value 1")

            entree(indexThread).WriteLine("isready")
            chaine = ""
            While InStr(chaine, "readyok") = 0
                If processus(indexThread).HasExited Then
                    entree(indexThread).Close()
                    sortie(indexThread).Close()
                    processus(indexThread).Close()
                    GoTo chargement_moteur
                End If
                chaine = sortie(indexThread).ReadLine
                Threading.Thread.Sleep(10)
            End While
        Catch ex As Exception
            If processus(indexThread).HasExited Then
                entree(indexThread).Close()
                sortie(indexThread).Close()
                processus(indexThread).Close()
                GoTo chargement_moteur
            End If
        End Try

    End Sub

    Public Function cpu(Optional reel As Boolean = False) As Integer
        Dim collection As New ManagementObjectSearcher("select * from Win32_Processor"), taches As Integer
        taches = 0

        For Each element As ManagementObject In collection.Get
            If reel Then
                taches = taches + element.Properties("NumberOfCores").Value 'cores
            Else
                taches = taches + element.Properties("NumberOfLogicalProcessors").Value 'threads
            End If
        Next

        Return taches
    End Function

    Public Sub dechargerMoteur(indexThread As Integer)
        Try
            entree(indexThread).Close()
            sortie(indexThread).Close()
            processus(indexThread).Close()
        Catch ex As Exception

        End Try

        entree(indexThread) = Nothing
        sortie(indexThread) = Nothing
        processus(indexThread) = Nothing
    End Sub

    Public Function gauche(texte As String, longueur As Integer) As String
        If longueur > 0 Then
            Return VB.Left(texte, longueur)
        Else
            Return ""
        End If
    End Function

    Public Function listerCoupsLegaux(position As String, indexThread As Integer) As String
        Dim chaine As String, liste As String, tabChaine() As String

        'on cherche tous les coups possibles
        entree(indexThread).WriteLine("setoption name MultiPV value " & maxMultiPVMoteur(moteur_court))

        If position = "" Then
            entree(indexThread).WriteLine("position startpos")
        ElseIf InStr(position, "/", CompareMethod.Text) > 0 _
          And (InStr(position, " w ", CompareMethod.Text) > 0 Or InStr(position, " b ", CompareMethod.Text) > 0) Then
            entree(indexThread).WriteLine("position fen " & position)
        Else
            entree(indexThread).WriteLine("position startpos moves " & position)
        End If

        entree(indexThread).WriteLine("go depth 1")

        chaine = ""
        liste = ""
        While InStr(chaine, "bestmove", CompareMethod.Text) = 0
            chaine = sortie(indexThread).ReadLine
            If InStr(chaine, " pv ", CompareMethod.Text) > 0 Then
                tabChaine = Split(chaine, " ")
                For i = 0 To UBound(tabChaine) - 1
                    If InStr(tabChaine(i), "pv", CompareMethod.Text) > 0 And tabChaine(i + 1) <> "" And Len(tabChaine(i + 1)) = 4 Then
                        'info depth 0 seldepth 0 multipv 0 score cp 0 nodes 214 nps 71333 tbhits 369 time 3 pv d7d5
                        'move=d7d5 weight=100%"
                        liste = liste & "move=" & tabChaine(i + 1) & " weight=-100%" & vbCrLf
                    End If
                Next
            End If
            Threading.Thread.Sleep(1)
        End While
        entree(indexThread).WriteLine("stop")

        entree(indexThread).WriteLine("isready")
        chaine = ""
        While InStr(chaine, "readyok") = 0
            chaine = sortie(indexThread).ReadLine
            Threading.Thread.Sleep(1)
        End While

        entree(indexThread).WriteLine("setoption name MultiPV value 1")

        entree(indexThread).WriteLine("isready")
        chaine = ""
        While InStr(chaine, "readyok") = 0
            chaine = sortie(indexThread).ReadLine
            Threading.Thread.Sleep(1)
        End While

        Return liste
    End Function

    Public Function maxMultiPVMoteur(chaine As String) As Integer
        maxMultiPVMoteur = 200
        If InStr(chaine, "asmfish", CompareMethod.Text) > 0 Then
            maxMultiPVMoteur = 224
        ElseIf InStr(chaine, "brainfish", CompareMethod.Text) > 0 _
            Or InStr(chaine, "brainlearn", CompareMethod.Text) > 0 _
            Or InStr(chaine, "stockfish", CompareMethod.Text) > 0 _
            Or InStr(chaine, "cfish", CompareMethod.Text) > 0 _
            Or InStr(chaine, "sugar", CompareMethod.Text) > 0 _
            Or InStr(chaine, "eman", CompareMethod.Text) > 0 _
            Or InStr(chaine, "hypnos", CompareMethod.Text) > 0 _
            Or InStr(chaine, "judas", CompareMethod.Text) > 0 _
            Or InStr(chaine, "aurora", CompareMethod.Text) > 0 Then
            maxMultiPVMoteur = 500
        ElseIf InStr(chaine, "houdini", CompareMethod.Text) > 0 Then
            maxMultiPVMoteur = 220
        ElseIf InStr(chaine, "komodo", CompareMethod.Text) > 0 Then
            maxMultiPVMoteur = 218
        End If

        Return maxMultiPVMoteur
    End Function

    Public Function nbCaracteres(ByVal chaine As String, ByVal critere As String) As Integer
        Return Len(chaine) - Len(Replace(chaine, critere, ""))
    End Function

    Public Function nomFichier(chemin As String) As String
        Return My.Computer.FileSystem.GetName(chemin)
    End Function

    Public Sub pgnUCI(chemin As String, fichier As String, suffixe As String, Optional priorite As Integer = 64)
        Dim nom As String, commande As New Process()
        Dim dossierFichier As String, dossierTravail As String

        nom = Replace(nomFichier(fichier), ".pgn", "")

        dossierFichier = fichier.Substring(0, fichier.LastIndexOf("\"))
        dossierTravail = My.Computer.FileSystem.GetParentPath(chemin)

        'si pgn-extract.exe ne se trouve à l'emplacement prévu (par <nom_ordinateur>.ini)
        If Not My.Computer.FileSystem.FileExists(dossierTravail & "\pgn-extract.exe") Then

            'si pgn-extract.exe ne se trouve dans le même dossier que le notre application
            dossierTravail = Environment.CurrentDirectory
            If Not My.Computer.FileSystem.FileExists(dossierTravail & "\pgn-extract.exe") Then

                'on cherche s'il se trouve dans le même dossier que le fichierPGN
                dossierTravail = dossierFichier
                If Not My.Computer.FileSystem.FileExists(dossierTravail & "\pgn-extract.exe") Then

                    'pgn-extract.exe est introuvable
                    MsgBox("Veuillez copier pgn-extract.exe dans :" & vbCrLf & dossierTravail, MsgBoxStyle.Critical)
                    dossierTravail = Environment.CurrentDirectory
                    If Not My.Computer.FileSystem.FileExists(dossierTravail & "\pgn-extract.exe") Then
                        End
                    End If
                End If
            End If

        End If

        'si le fichierPGN ne se trouve pas dans le dossier de travail
        If dossierFichier <> dossierTravail Then
            'on recopie temporairement le fichierPGN dans le dossierTravail
            My.Computer.FileSystem.CopyFile(fichier, dossierTravail & "\" & nom & ".pgn", True)
        End If

        commande.StartInfo.FileName = dossierTravail & "\pgn-extract.exe"
        commande.StartInfo.WorkingDirectory = dossierTravail

        If InStr(nom, " ") = 0 Then
            commande.StartInfo.Arguments = " -s -Wuci -o" & nom & suffixe & ".pgn" & " " & nom & ".pgn"
        Else
            commande.StartInfo.Arguments = " -s -Wuci -o""" & nom & suffixe & ".pgn""" & " """ & nom & ".pgn"""
        End If

        commande.StartInfo.CreateNoWindow = True
        commande.StartInfo.UseShellExecute = False
        commande.Start()
        commande.PriorityClass = priorite '64 (idle), 16384 (below normal), 32 (normal), 32768 (above normal), 128 (high), 256 (realtime)
        commande.WaitForExit()

        'si le dossierTravail ne correspond pas au dossier du fichierPGN
        If dossierFichier <> dossierTravail Then
            'on déplace le fichier moteur
            Try
                My.Computer.FileSystem.DeleteFile(dossierTravail & "\" & nom & ".pgn")
            Catch ex As Exception

            End Try
            My.Computer.FileSystem.MoveFile(dossierTravail & "\" & nom & suffixe & ".pgn", dossierFichier & "\" & nom & suffixe & ".pgn")
        End If
    End Sub

    Public Function positionToEPD(position As String, indexThread As Integer) As String
        Dim positionEPD As String

        positionEPD = ""

        If position <> "" Then
            entree(indexThread).WriteLine("position startpos moves " & position)
        End If
        entree(indexThread).WriteLine("d")
        While InStr(positionEPD, "Fen: ", CompareMethod.Text) = 0
            positionEPD = sortie(indexThread).ReadLine
        End While
        positionEPD = Replace(positionEPD, "Fen: ", "")
        entree(indexThread).WriteLine("stop")

        Return positionEPD
    End Function
End Module
