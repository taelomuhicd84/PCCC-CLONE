;;; TKPCCC_BotChat.lsp - nap plugin Bot chat (TKPCCC.BotChat.dll) ma khong can go NETLOAD. File nay chi dung chu ASCII.
;;; Cach dung: APPLOAD -> chon file nay (bam "Contents..." trong Startup Suite de tu nap moi lan mo AutoCAD).
;;; Sau khi nap: go TKBOTCHAT (mo khung chat) hoac TKBOTCHON (quet vung gui kem). Go lenh TKNAPBOT de nap lai.
;;; Tim DLL theo thu tu: ApplicationPlugins (APPDATA, ProgramData) -> canh thu muc 07_CAD (suy ra tu bien moi truong
;;; TKPCCC_DLL do 07_CAD\cai_dat.bat dat) -> Support File Search Path -> hop thoai chon file.
;;; Neu SECURELOAD = 1 va thu muc DLL chua nam trong TRUSTEDPATHS, AutoCAD hoi mot lan: chon "Always Load".

(defun tkbot:tim-dll (/ ds r pcc cad)
  (setq pcc (getenv "TKPCCC_DLL")) ; ...\07_CAD\TKPCCC.bundle\Contents\TKPCCC.Cad.dll
  (if (and pcc (/= pcc ""))
    (setq cad (vl-filename-directory (vl-filename-directory (vl-filename-directory pcc)))))
  (setq ds (list (strcat (getenv "APPDATA") "\\Autodesk\\ApplicationPlugins\\TKPCCC_BotChat.bundle\\Contents\\TKPCCC.BotChat.dll")
                 (strcat (getenv "ProgramData") "\\Autodesk\\ApplicationPlugins\\TKPCCC_BotChat.bundle\\Contents\\TKPCCC.BotChat.dll")
                 (if cad (strcat cad "\\TKPCCC_BotChat.bundle\\Contents\\TKPCCC.BotChat.dll"))
                 "TKPCCC.BotChat.dll"))
  (foreach d ds
    (if (and (not r) d (findfile d)) (setq r (findfile d))))
  (if (not r)
    (setq r (getfiled "Chon TKPCCC.BotChat.dll (07_CAD\\TKPCCC_BotChat.bundle\\Contents)" "" "dll" 4)))
  r)

(defun c:TKNAPBOT (/ dll cu)
  (if (setq dll (tkbot:tim-dll))
    (progn
      (setq cu (getvar "CMDECHO"))
      (setvar "CMDECHO" 0)
      (command "_.NETLOAD" dll)
      (setvar "CMDECHO" cu)
      (princ (strcat "\n[TKPCCC] Da nap " dll ". Go TKBOTCHAT de mo Bot chat.")))
    (princ "\n[TKPCCC] Khong tim thay TKPCCC.BotChat.dll. Chay 07_CAD\\build.bat roi 07_CAD\\cai_dat.bat."))
  (princ))

;; Nap mot lan cho ca phien AutoCAD (nho tren blackboard). Luc AutoCAD dang khoi dong ban ve chua goi duoc (command): doi S::STARTUP.
(defun tkbot:tu-nap ()
  (if (not (vl-bb-ref 'tkbot:da-nap))
    (progn (c:TKNAPBOT) (vl-bb-set 'tkbot:da-nap T))))
(if (vl-catch-all-error-p (vl-catch-all-apply 'tkbot:tu-nap nil))
  (vl-catch-all-apply
    '(lambda ()
       (defun-q tkbot:khoi-dong () (tkbot:tu-nap))
       (setq S::STARTUP (append S::STARTUP tkbot:khoi-dong)))))
(princ)
