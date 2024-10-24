import {
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  OnInit,
  TemplateRef,
  ViewChild,
} from "@angular/core";
import { ActivatedRoute } from "@angular/router";

import { LoggerService } from "src/app/core/services/logger.service";
import { UserStore } from "./user-store.service";
import {
  FileParameter,
  IdentityApiService,
  SingleUserInfo,
  UiFeaturesInfo,
} from "src/app/core/services/identity-api.service";
import { UiFeaturesService } from "src/app/core/services/ui-features.service";
import { forkJoin, map, Subscription } from "rxjs";
import { NgbModal, NgbModalRef } from "@ng-bootstrap/ng-bootstrap";
import { environment } from "src/environments/environment";

@Component({
  selector: "app-user-edit",
  templateUrl: "./user-edit.component.html",
  styleUrls: ["./user-edit.component.scss"],
  providers: [UserStore],
})
export class UserEditComponent implements OnInit, OnDestroy {
  @ViewChild("content", { static: false })
  contentRef!: TemplateRef<HTMLDivElement>;
  @ViewChild("fileInput", { static: true })
  fileInputRef!: ElementRef<HTMLInputElement>;

  public userId = "";
  public userName = "";
  public displayName = "";
  public avatar = "";
  public signInLogsEnabled = false;

  public modalRef: NgbModalRef;
  public canvas: HTMLCanvasElement;

  public scale = 1;
  public minScale = 1;
  public maxScale = 1;

  public canvasWidth = 400;
  public canvasHeight = 400;

  private ctx!: CanvasRenderingContext2D;
  private img!: HTMLImageElement;

  private panX = 0;
  private panY = 0;

  private posX = 0;
  private posY = 0;
  private isPanning = false;
  private initialDistance = 0;

  private _getDataSubscription: Subscription;

  constructor(
    private route: ActivatedRoute,
    private logger: LoggerService,
    private uiFeaturesService: UiFeaturesService,
    private userStore: UserStore,
    private apiService: IdentityApiService,
    private modalService: NgbModal
  ) {}

  public ngOnInit(): void {
    this.logger.log("UserEditComponent ngOnInit was called.");
    this.userId = this.route.snapshot.params["id"];

    const getFeatures = this.uiFeaturesService.getUiFeatures();
    const getUser = this.userStore.getUser(this.userId);

    this._getDataSubscription = forkJoin([getFeatures, getUser])
      .pipe(
        map((responses: [UiFeaturesInfo, SingleUserInfo]) => {
          return {
            features: responses[0],
            user: responses[1],
          };
        })
      )
      .subscribe(
        (result: { user: SingleUserInfo; features: UiFeaturesInfo }) => {
          this.signInLogsEnabled = result.features.signInLogsEnabled;

          const { userName, claims } = result.user;
          const givenName = claims.find((c) => c.type === "given_name");
          const familyName = claims.find((c) => c.type === "family_name");

          this.userName = userName;
          this.displayName = `${givenName.value} ${familyName.value}`;
          // this.avatar = `${environment.api_url}/pictures/${this.userId}?d=/avatar/${this.displayName}/512.png`;
        }
      );
  }

  public ngOnDestroy(): void {
    this.logger.log("UserEditComponent ngOnDestroy was called.");
    if (this._getDataSubscription) {
      this._getDataSubscription.unsubscribe();
    }
  }

  public openModal(): void {
    this.modalRef = this.modalService.open(this.contentRef);
    this.modalRef.shown.subscribe((_) => {
      this.canvas = document.getElementById("canvas") as HTMLCanvasElement;
      this.ctx = this.canvas.getContext("2d")!;

      this.drawImage();
    });
  }

  public closeModal(): void {
    if (this.modalRef) {
      this.modalRef.close(0);
    }

    this.resetImagePosition();
    this.scale = this.minScale;
    this.panX = 0;
    this.panY = 0;

    this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

    this.fileInputRef.nativeElement.value = "";
  }

  public onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files ? input.files[0] : null;
    if (file) {
      const reader = new FileReader();
      reader.onload = (e: ProgressEvent<FileReader>) => {
        this.img = new Image();
        this.img.src = e.target?.result as string;
        this.img.onload = () => {
          this.calculateMinAndMaxScale();
          this.resetImagePosition();
          // this.drawImage();

          this.openModal();
        };
      };
      reader.readAsDataURL(file);
    }
  }

  public calculateMinAndMaxScale(): void {
    const scaleWidth = this.img.width / this.canvasWidth;
    const scaleHeight = this.img.height / this.canvasHeight;

    // const isLoRes =
    //   this.img.width <= this.canvasWidth &&
    //   this.img.height <= this.canvasHeight;

    // this.minScale = 1;
    // if (this.img.width === this.img.height) {
    this.minScale = Math.min(
      this.canvasWidth / this.img.width,
      this.canvasHeight / this.img.height
    );
    // }
    // if (isLoRes) {
    //   this.minScale = Math.min(
    //     this.canvasWidth / this.img.width,
    //     this.canvasHeight / this.img.height
    //   );
    // }
    this.maxScale = Math.max(scaleWidth, scaleHeight);
    this.scale = this.minScale;
  }

  public resetImagePosition(): void {
    this.panX = (this.canvasWidth - this.img.width * this.scale) / 2;
    this.panY = (this.canvasHeight - this.img.height * this.scale) / 2;
  }

  public drawImage(): void {
    this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
    this.ctx.save();
    this.ctx.translate(this.panX, this.panY);
    this.ctx.scale(this.scale, this.scale);
    this.ctx.drawImage(this.img, 0, 0);
    this.ctx.restore();
  }

  public onZoomChange(event: Event): void {
    const target = event.target as HTMLInputElement;
    const newScale = parseFloat(target.value);

    this.zoomAtCenter(newScale);
  }

  public zoomAtCenter(newScale: number): void {
    const canvasCenterX = this.canvas.width / 2;
    const canvasCenterY = this.canvas.height / 2;

    const imgCenterX = (canvasCenterX - this.panX) / this.scale;
    const imgCenterY = (canvasCenterY - this.panY) / this.scale;

    this.scale = Math.min(this.maxScale, Math.max(this.minScale, newScale));
    this.panX = canvasCenterX - imgCenterX * this.scale;
    this.panY = canvasCenterY - imgCenterY * this.scale;

    this.drawImage();
  }

  public onPanStart(event: MouseEvent | TouchEvent): void {
    if (event instanceof MouseEvent) {
      this.isPanning = true;
      this.posX = event.clientX;
      this.posY = event.clientY;
    } else if (event instanceof TouchEvent && event.touches.length === 1) {
      this.isPanning = true;
      this.posX = event.touches[0].clientX;
      this.posY = event.touches[0].clientY;
    } else if (event instanceof TouchEvent && event.touches.length === 2) {
      this.initialDistance = this.getTouchDistance(event);
    }
  }

  public onPan(event: MouseEvent | TouchEvent): void {
    event.preventDefault();

    if (this.isPanning) {
      let dx = 0;
      let dy = 0;

      if (event instanceof MouseEvent) {
        dx = event.clientX - this.posX;
        dy = event.clientY - this.posY;

        this.posX = event.clientX;
        this.posY = event.clientY;
      } else if (event instanceof TouchEvent && event.touches.length === 1) {
        dx = event.touches[0].clientX - this.posX;
        dy = event.touches[0].clientY - this.posY;

        this.posX = event.touches[0].clientX;
        this.posY = event.touches[0].clientY;
      }

      this.panX += dx;
      this.panY += dy;
      this.drawImage();
    } else if (event instanceof TouchEvent && event.touches.length === 2) {
      const currentDistance = this.getTouchDistance(event);
      const scaleChange = currentDistance / this.initialDistance;
      this.zoomAtCenter(this.scale * scaleChange);
      this.initialDistance = currentDistance;
    }
  }

  @HostListener("window:mouseup")
  @HostListener("window:touchend")
  public onPanEnd(): void {
    this.isPanning = false;
  }

  public getTouchDistance(event: TouchEvent): number {
    const touch1 = event.touches[0];
    const touch2 = event.touches[1];

    const dx = touch1.clientX - touch2.clientX;
    const dy = touch1.clientY - touch2.clientY;

    return Math.sqrt(dx * dx + dy * dy);
  }

  public cropImage(): void {
    const cropWidth = this.canvasWidth / this.scale;
    const cropHeight = this.canvasHeight / this.scale;

    const cropX = -this.panX / this.scale;
    const cropY = -this.panY / this.scale;

    const offScreenCanvas = document.createElement("canvas");
    offScreenCanvas.width = cropWidth;
    offScreenCanvas.height = cropHeight;
    const offScreenCtx = offScreenCanvas.getContext("2d")!;

    offScreenCtx.drawImage(
      this.img,
      cropX,
      cropY,
      cropWidth,
      cropHeight,
      0,
      0,
      cropWidth,
      cropHeight
    );

    const croppedImageURL = offScreenCanvas.toDataURL("image/png", 1.0);
    this.upload(croppedImageURL);
    // this.profileImageUrl = croppedImageURL;
  }

  private upload(imageData: string) {
    const data = this.dataURLtoBlob(imageData);
    const fileName = `${this.userId}.png`;

    const file: FileParameter = {
      data,
      fileName,
    };

    this.apiService.saveUserPicture(this.userId, file).subscribe((response) => {
      this.avatar = imageData;
      this.closeModal();
    });
  }

  private dataURLtoBlob(dataUrl) {
    const byteString = atob(dataUrl.split(",")[1]);
    const mimeString = dataUrl.split(",")[0].split(":")[1].split(";")[0];
    const ab = new ArrayBuffer(byteString.length);
    const ia = new Uint8Array(ab);

    for (let i = 0; i < byteString.length; i++) {
      ia[i] = byteString.charCodeAt(i);
    }

    return new Blob([ab], { type: mimeString });
  }
}
