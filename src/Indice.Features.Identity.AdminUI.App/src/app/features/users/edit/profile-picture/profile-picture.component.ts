import {
  Component,
  Input,
  OnDestroy,
  TemplateRef,
  ViewChild,
} from "@angular/core";
import { NgbModal, NgbModalRef } from "@ng-bootstrap/ng-bootstrap";
import { Subscription } from "rxjs";
import {
  FileParameter,
  IdentityApiService,
} from "src/app/core/services/identity-api.service";
import { ToastService } from "src/app/layout/services/app-toast.service";

// TODO: add touch event and pinch to zoom
@Component({
  selector: "app-user-profile-picture",
  templateUrl: "./profile-picture.component.html",
  styleUrls: ["./profile-picture.component.scss"],
})
export class UserProfilePictureComponent implements OnDestroy {
  @ViewChild("modalContent", { static: false })
  modalContent!: TemplateRef<HTMLDivElement>;

  @Input() public userId: string;
  @Input() public displayName: string;

  modalRef: NgbModalRef;

  viewPort: number = 400;
  viewPortOrientation: "landscape" | "portrait" | "square" = "square";
  step: number = 0.01;
  scale: number = 1;
  minScale: number = 1;
  maxScale: number = 1;
  initialTranslateX: number = 0;
  initialTranslateY: number = 0;
  translateX: number = 0;
  translateY: number = 0;
  dragCoords: { x: number; y: number } | null = null;

  initialLeftBound = 0;
  initialRightBound = 0;
  initialTopBound = 0;
  initialBottomBound = 0;

  leftBound = 0;
  rightBound = 0;
  topBound = 0;
  bottomBound = 0;

  image: HTMLImageElement;
  imageURL: string = "";

  file: File;
  fileParameter: FileParameter;
  fileVersion = 1;

  apiSubscription: Subscription;
  modalSubscription: Subscription;

  constructor(
    private api: IdentityApiService,
    private toast: ToastService,
    private modalService: NgbModal
  ) {}

  public ngOnDestroy(): void {
    if (this.apiSubscription) {
      this.apiSubscription.unsubscribe();
    }
    if (this.modalSubscription) {
      this.modalSubscription.unsubscribe();
    }
  }

  get isSquare(): boolean {
    return this.viewPortOrientation === "square";
  }

  get isPortrait(): boolean {
    return this.viewPortOrientation === "portrait";
  }

  get isLandscape(): boolean {
    return this.viewPortOrientation === "landscape";
  }

  public onFileSelected(event: Event): void {
    this.file = (event.target as HTMLInputElement).files?.[0];
    if (this.file) {
      this.fileParameter = {
        fileName: this.file.name,
        data: this.file,
      };

      this.openModal();
    }
  }

  public onDragStart(event: MouseEvent | TouchEvent): void {
    event.preventDefault();

    if (!this.dragCoords) {
      const clientX =
        event instanceof MouseEvent ? event.clientX : event.touches[0].clientX;
      const clientY =
        event instanceof MouseEvent ? event.clientY : event.touches[0].clientY;

      this.dragCoords = {
        x: clientX - this.translateX,
        y: clientY - this.translateY,
      };
    }
  }

  public onDrag(event: MouseEvent | TouchEvent): void {
    if (this.dragCoords) {
      event.preventDefault();

      const clientX =
        event instanceof MouseEvent ? event.clientX : event.touches[0].clientX;
      const clientY =
        event instanceof MouseEvent ? event.clientY : event.touches[0].clientY;

      this.translateX = clientX - this.dragCoords.x;
      this.translateY = clientY - this.dragCoords.y;

      this.translateY = Math.max(this.translateY, this.bottomBound);
      this.translateY = Math.min(this.translateY, this.topBound);
      this.translateX = Math.max(this.translateX, this.rightBound);
      this.translateX = Math.min(this.translateX, this.leftBound);

      this.transform();
    }
  }

  public onDragEnd(): void {
    this.dragCoords = null;
  }

  public onMouseWheel(event: WheelEvent): void {
    event.preventDefault();

    this.scale += event.deltaY < 0 ? this.step : -this.step;
    this.scale = Math.max(this.minScale, Math.min(this.scale, this.maxScale));

    this.calculateBounds();
    this.transform();
  }

  public onScale(newScale: number): void {
    this.scale = newScale;

    this.calculateBounds();
    this.transform();
  }

  public openModal(): void {
    this.modalRef = this.modalService.open(this.modalContent);
    this.modalSubscription = this.modalRef.shown.subscribe((_) => {
      this.image = document.getElementById("pic") as HTMLImageElement;
      this.image.src = this.imageURL = URL.createObjectURL(this.file);
      this.image.onload = () => {
        this.setup();
      };
    });
  }

  public closeModal(): void {
    if (this.modalRef) {
      this.reset();
      this.modalRef.close();
    }
  }

  private calculateBounds(): void {
    const scaledHeight = this.image.height * this.scale;
    const horizontalOverflow = (scaledHeight - this.viewPort) / 2;

    switch (this.viewPortOrientation) {
      case "square":
      case "landscape":
        this.leftBound = Math.round(horizontalOverflow / this.scale);
        this.rightBound = -this.leftBound;
        this.topBound = Math.round(this.leftBound - this.initialLeftBound);
        this.bottomBound = -this.topBound;
        break;
      case "portrait":
        this.topBound = Math.round(
          horizontalOverflow / this.scale - this.initialLeftBound
        );
        this.bottomBound = -this.topBound;
        this.leftBound = this.topBound - this.initialTopBound;
        this.rightBound = -this.leftBound;
        break;
    }
  }

  private transform(): void {
    this.image.style.transform = `scale(${this.scale}) translate(${this.translateX}px, ${this.translateY}px)`;
  }

  private setup(): void {
    const { naturalWidth, naturalHeight } = this.image;
    const scaleFactor = this.viewPort / this.image.height;
    const scaledWidth = this.image.width * scaleFactor;

    if (naturalHeight > naturalWidth) {
      this.configurePortraitViewOrientation(scaledWidth);
    } else if (naturalWidth > naturalHeight) {
      this.configureLandscapeViewOrientation(scaledWidth);
    } else {
      this.configureSquareViewOrientation();
    }

    this.minScale = this.scale;

    this.calculateBounds();
    this.transform();
  }

  private configurePortraitViewOrientation(scaledWidth: number): void {
    const ratio = this.viewPort / scaledWidth;
    this.viewPortOrientation = "portrait";
    this.scale = (this.image.naturalHeight / this.image.naturalWidth) * ratio;
    this.maxScale = this.image.naturalWidth / this.viewPort;

    const horizontalOverflow =
      (this.image.height * this.scale - this.viewPort) / 2;
    this.initialTopBound = Math.round(horizontalOverflow / this.scale);
    this.initialBottomBound = -this.initialTopBound;
    this.initialLeftBound = 0;
    this.initialRightBound = 0;
  }

  private configureLandscapeViewOrientation(scaledWidth: number): void {
    this.viewPortOrientation = "landscape";
    this.scale =
      (this.image.naturalWidth / this.image.naturalHeight) * this.scale;
    this.maxScale = this.image.naturalHeight / this.viewPort;

    this.translateX = -scaledWidth / 2 + this.viewPort / 2;
    this.translateY = 0;
    this.initialTranslateX = this.translateX;
    this.initialTranslateY = this.translateY;

    const horizontalOverflow =
      (this.image.height * this.scale - this.viewPort) / 2;
    this.initialLeftBound = Math.round(horizontalOverflow / this.scale);
    this.initialRightBound = -this.initialLeftBound;
  }

  private configureSquareViewOrientation(): void {
    this.viewPortOrientation = "square";
    this.scale =
      (this.image.naturalWidth / this.image.naturalHeight) * this.scale;
    this.maxScale = this.image.naturalHeight / this.viewPort;
  }

  public crop(): void {
    this.apiSubscription = this.api
      .saveUserPicture(
        this.userId,
        this.fileParameter,
        this.scale,
        this.translateX,
        this.translateY,
        this.viewPort
      )
      .subscribe({
        next: (_) => {
          this.fileVersion++;
          this.toast.showSuccess(
            "Your profile picture was updated successfully!"
          );
          this.closeModal();
        },
        error: (_) => {
          this.toast.showDanger(
            "There was an error updating your profile picture."
          );
          this.closeModal();
        },
      });
  }

  private reset(): void {
    this.viewPortOrientation = "square";
    this.scale = 1;
    this.minScale = 1;
    this.maxScale = 1;
    this.initialTranslateX = 0;
    this.initialTranslateY = 0;
    this.translateX = 0;
    this.translateY = 0;
    this.initialLeftBound = 0;
    this.initialRightBound = 0;
    this.initialTopBound = 0;
    this.initialBottomBound = 0;
    this.leftBound = 0;
    this.rightBound = 0;
    this.topBound = 0;
    this.bottomBound = 0;
    this.image = null;
    this.imageURL = "";
    this.file = null;
    this.fileParameter = null;
  }
}