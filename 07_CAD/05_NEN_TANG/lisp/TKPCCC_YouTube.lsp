;;; TKPCCC_YouTube.lsp - nap plugin YouTube (TKPCCC.YouTube.dll) ma khong can go NETLOAD. File nay chi dung chu ASCII.
;;; Cach dung: APPLOAD -> chon file nay (bam "Contents..." trong Startup Suite de tu nap moi lan mo AutoCAD).
;;; Sau khi nap: go TKYOUTUBE. Go lenh TKNAPYT de nap lai.
;;; Tim DLL theo thu tu: ApplicationPlugins (APPDATA, ProgramData) -> canh thu muc 07_CAD (suy ra tu bien moi truong
;;; TKPCCC_DLL do 07_CAD\cai_dat.bat dat) -> Support File Search Path -> hop thoai chon file.
;;; Neu SECURELOAD = 1 va thu muc DLL chua nam trong TRUSTEDPATHS, AutoCAD hoi mot lan: chon "Always Load".

(defun tkyt:tim-dll (/ ds r pcc cad)
  (setq pcc (getenv "TKPCCC_DLL")) ; ...\07_CAD\TKPCCC.bundle\Contents\TKPCCC.Cad.dll
  (if (and pcc (/= pcc ""))
    (setq cad (vl-filename-directory (vl-filename-directory (vl-filename-directory pcc)))))
  (setq ds (list (strcat (getenv "APPDATA") "\\Autodesk\\ApplicationPlugins\\TKPCCC_YouTube.bundle\\Contents\\TKPCCC.YouTube.dll")
                 (strcat (getenv "ProgramData") "\\Autodesk\\ApplicationPlugins\\TKPCCC_YouTube.bundle\\Contents\\TKPCCC.YouTube.dll")
                 (if cad (strcat cad "\\TKPCCC_YouTube.bundle\\Contents\\TKPCCC.YouTube.dll"))
                 "TKPCCC.YouTube.dll"))
  (foreach d ds
    (if (and (not r) d (findfile d)) (setq r (findfile d))))
  (if (not r)
    (setq r (getfiled "Chon TKPCCC.YouTube.dll (07_CAD\\TKPCCC_YouTube.bundle\\Contents)" "" "dll" 4)))
  r)

(defun c:TKNAPYT (/ dll cu)
  (if (setq dll (tkyt:tim-dll))
    (progn
      (setq cu (getvar "CMDECHO"))
      (setvar "CMDECHO" 0)
      (command "_.NETLOAD" dll)
      (setvar "CMDECHO" cu)
      (princ (strcat "\n[TKPCCC] Da nap " dll ". Go TKYOUTUBE de mo YouTube.")))
    (princ "\n[TKPCCC] Khong tim thay TKPCCC.YouTube.dll. Chay 07_CAD\\build.bat roi 07_CAD\\cai_dat.bat."))
  (princ))

;; Nap mot lan cho ca phien AutoCAD (nho tren blackboard). Luc AutoCAD dang khoi dong ban ve chua goi duoc (command): doi S::STARTUP.
(defun tkyt:tu-nap ()
  (if (not (vl-bb-ref 'tkyt:da-nap))
    (progn (c:TKNAPYT) (vl-bb-set 'tkyt:da-nap T))))
(if (vl-catch-all-error-p (vl-catch-all-apply 'tkyt:tu-nap nil))
  (vl-catch-all-apply
    '(lambda ()
       (defun-q tkyt:khoi-dong () (tkyt:tu-nap))
       (setq S::STARTUP (append S::STARTUP tkyt:khoi-dong)))))
(princ)
