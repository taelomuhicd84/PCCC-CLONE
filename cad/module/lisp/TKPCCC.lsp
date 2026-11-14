;;; TKPCCC.lsp - nap plugin TKPCCC ma khong can go NETLOAD. File nay chi dung chu ASCII.
;;; Cach dung:
;;;   1. APPLOAD -> chon file nay (bam "Contents..." trong Startup Suite de tu nap moi lan mo AutoCAD).
;;;   2. Hoac go lenh TKPCCC bat cu luc nao de nap lai.
;;; Tim TKPCCC.Cad.dll theo thu tu: bien moi truong TKPCCC_DLL (cad\cai_dat.bat dat) ->
;;;   %APPDATA%\Autodesk\ApplicationPlugins\TKPCCC.bundle -> %ProgramData%\... -> Support File Search Path.
;;; TKPCCC.Cad.dll tu nap TKPCCC.UI.dll (bang PCCC). Neu SECURELOAD = 1 va thu muc DLL chua nam trong
;;; TRUSTEDPATHS, AutoCAD se hoi mot lan: chon "Always Load".

(defun tkpccc:tim-dll (/ ds r)
  (setq ds (list (getenv "TKPCCC_DLL")
                 (strcat (getenv "APPDATA") "\\Autodesk\\ApplicationPlugins\\TKPCCC.bundle\\Contents\\TKPCCC.Cad.dll")
                 (strcat (getenv "ProgramData") "\\Autodesk\\ApplicationPlugins\\TKPCCC.bundle\\Contents\\TKPCCC.Cad.dll")
                 "TKPCCC.Cad.dll"))
  (foreach d ds
    (if (and (not r) d (/= d "") (findfile d)) (setq r (findfile d))))
  r)

;; Nap mot lan cho ca phien AutoCAD (co nho tren blackboard, song qua moi ban ve). ep = T: nap lai du da nap.
(defun tkpccc:nap (ep / dll cu)
  (cond
    ((and (not ep) (vl-bb-ref 'tkpccc:da-nap)) nil)
    ((setq dll (tkpccc:tim-dll))
     (setq cu (getvar "CMDECHO"))
     (setvar "CMDECHO" 0)
     (command "_.NETLOAD" dll)
     (setvar "CMDECHO" cu)
     (vl-bb-set 'tkpccc:da-nap T)
     (princ (strcat "\n[TKPCCC] da nap " dll ". Go PCCC de mo bang.")))
    (T (princ "\n[TKPCCC] Khong tim thay TKPCCC.Cad.dll. Chay cad\\build.bat roi cad\\cai_dat.bat, hoac dat bien moi truong TKPCCC_DLL.")))
  (princ))

(defun c:TKPCCC () (tkpccc:nap T))

;; Nap ngay khi file nay duoc load. Luc AutoCAD dang khoi dong ban ve (acaddoc.lsp) chua goi duoc (command):
;; khi do doi S::STARTUP.
(if (vl-catch-all-error-p (vl-catch-all-apply 'tkpccc:nap (list nil)))
  (vl-catch-all-apply
    '(lambda ()
       (defun-q tkpccc:khoi-dong () (tkpccc:nap nil))
       (setq S::STARTUP (append S::STARTUP tkpccc:khoi-dong)))))
(princ)
