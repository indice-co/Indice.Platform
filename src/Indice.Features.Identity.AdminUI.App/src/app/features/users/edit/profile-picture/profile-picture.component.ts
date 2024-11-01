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

// TODO: add touch event and pinch to zoom
// TODO: calculate bounds when zoom in/out
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
  modalSubscription: Subscription;

  viewPort: number = 400;
  viewPortOrientation: "landscape" | "portrait" | "square" = "square";
  scale: number = 1;
  scaleFit: number = 1;
  minScale: number = 1;
  maxScale: number = 1;
  panX: number = 0;
  panY: number = 0;
  initialTranslateX: number = 0;
  initialTranslateY: number = 0;
  translateX: number = 0;
  translateY: number = 0;
  dragCoords: { x: number; y: number } | null = null;

  leftBound = 0;
  rightBound = 0;
  topBound = 0;
  bottomBound = 0;

  image: HTMLImageElement;
  imageURL: string = "";

  file: File;
  fileVersion = 1;

  apiSubscription: Subscription;

  constructor(
    private api: IdentityApiService,
    private modalService: NgbModal
  ) {}

  public ngOnDestroy(): void {
    if (this.apiSubscription) {
      this.apiSubscription.unsubscribe();
    }
  }

  get isPortrait(): boolean {
    return this.viewPortOrientation === "portrait";
  }

  get isLandscape(): boolean {
    return this.viewPortOrientation === "landscape";
  }

  onSelectFile(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) {
      this.file = file;
      this.openModal();
    }
  }

  onTransform() {
    this.image.style.transform = `scale(${this.scale}) translate(${this.translateX}px, ${this.translateY}px)`;
  }

  reset() {
    this.viewPortOrientation = "square";
    this.scale = 1;
    this.scaleFit = 1;
    this.minScale = 1;
    this.maxScale = 1;
    this.panX = 0;
    this.panY = 0;
    this.initialTranslateX = 0;
    this.initialTranslateY = 0;
    this.translateX = 0;
    this.translateY = 0;
    this.leftBound = 0;
    this.rightBound = 0;
    this.topBound = 0;
    this.bottomBound = 0;
    this.image = null;
    this.imageURL = "";
    this.file = null;
  }

  dragStart(event: MouseEvent | TouchEvent) {
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

  drag(event: MouseEvent | TouchEvent) {
    if (this.dragCoords) {
      event.preventDefault();

      const clientX =
        event instanceof MouseEvent ? event.clientX : event.touches[0].clientX;
      const clientY =
        event instanceof MouseEvent ? event.clientY : event.touches[0].clientY;

      this.translateX = clientX - this.dragCoords.x;
      this.translateY = clientY - this.dragCoords.y;

      switch (this.viewPortOrientation) {
        case "square":
          if (Math.abs(this.translateX) > 0) {
            this.translateX = 0;
          }
          if (Math.abs(this.translateY) > 0) {
            this.translateY = 0;
          }
          break;
        case "portrait":
          if (this.translateY > this.topBound) {
            this.translateY = this.topBound;
          }
          if (this.translateY < this.bottomBound) {
            this.translateY = this.bottomBound;
          }
          this.translateX = 0;
          break;
        case "landscape":
          if (this.translateX > this.leftBound) {
            this.translateX = this.leftBound;
          }
          if (this.translateX < this.rightBound) {
            this.translateX = this.rightBound;
          }
          if (this.translateY > this.topBound) {
            this.translateY = this.topBound;
          }
          if (this.translateY < this.bottomBound) {
            this.translateY = this.bottomBound;
          }
          break;
      }

      this.panX = this.translateX - this.initialTranslateX;
      this.panY = this.translateY - this.initialTranslateY;

      this.onTransform();
    }
  }

  dragEnd() {
    this.dragCoords = null;
  }

  onMouseWheel(event: WheelEvent) {
    // event.preventDefault();
    // this.scale += event.deltaY < 0 ? 0.15 : -0.15;
    // this.scale = Math.max(this.minScale, this.scale);
    // this.calcBounds();
    // this.onTransform();
  }

  calcBounds() {
    const { width, height } = this.image;

    const scaledWidth = width * this.scale;
    const scaledHeight = height * this.scale;

    let translateXBounds = { left: 0, right: 0 };
    let translateYBounds = { top: 0, bottom: 0 };

    if (scaledWidth > this.viewPort) {
      const overflowX = (scaledWidth - this.viewPort) / 2;
      translateXBounds = {
        left: -overflowX,
        right: overflowX,
      };
    } else {
      translateXBounds.left = 0;
      translateXBounds.right = 0;
    }

    if (scaledHeight > this.viewPort) {
      const overflowY = (scaledHeight - this.viewPort) / 2;
      translateYBounds = {
        top: -overflowY,
        bottom: overflowY,
      };
    } else {
      translateYBounds.top = 0;
      translateYBounds.bottom = 0;
    }

    this.leftBound = translateXBounds.right + this.translateX;
    this.rightBound = translateXBounds.left + this.translateX;
    this.topBound = translateYBounds.top + this.translateY;
    this.bottomBound = translateYBounds.bottom + this.translateY;
  }

  setBounds() {
    const { width, height } = this.image;
    if (height > width) {
      const scaleFactor = this.viewPort / height;
      const scaledWidth = width * scaleFactor;

      this.viewPortOrientation = "portrait";
      this.scale = this.viewPort / scaledWidth;
      this.scaleFit = this.viewPort / this.image.naturalHeight;
      this.minScale = this.scale;

      this.topBound = -Math.round(
        Math.ceil(scaledWidth) / 2 - this.viewPort / 2
      );
      this.bottomBound = -this.topBound;
    }

    if (width > height) {
      const scaleFactor = this.viewPort / height;
      const scaledWidth = width * scaleFactor;

      this.viewPortOrientation = "landscape";
      this.scaleFit = this.viewPort / this.image.naturalWidth;
      this.translateX = -scaledWidth / 2 + this.viewPort / 2;
      this.translateY = 0;
      this.initialTranslateX = this.translateX;
      this.initialTranslateY = this.translateY;

      this.calcBounds();
    }

    if (width === height) {
      this.viewPortOrientation = "square";
      this.scaleFit = this.viewPort / this.image.naturalWidth;
      this.topBound = 0;
      this.bottomBound = 0;
      this.leftBound = 0;
      this.rightBound = 0;
    }
  }

  public openModal(): void {
    this.modalRef = this.modalService.open(this.modalContent);
    this.modalSubscription = this.modalRef.shown.subscribe((_) => {
      const image = document.getElementById("pic") as HTMLImageElement;
      image.src = this.imageURL = URL.createObjectURL(this.file);
      image.onload = () => {
        const imgWidth = image.naturalWidth;

        this.scale = 1;
        this.minScale = 1;
        this.maxScale = imgWidth / this.viewPort;
        this.translateX = 0;
        this.translateY = 0;
        this.image = image;

        this.setBounds();
        this.onTransform();
      };
    });
  }

  public closeModal(): void {
    if (this.modalRef) {
      this.reset();
      this.modalRef.close();
    }
  }

  public crop() {
    const { naturalWidth, naturalHeight, width, height } = this.image;
    let scale;
    switch (this.viewPortOrientation) {
      case "square":
      case "landscape":
        scale = naturalHeight / height;
        break;
      case "portrait":
        scale = naturalWidth / width;
        break;
    }
    const scaleFit = this.scale / this.scaleFit;
    const scaleRatio = scaleFit / scale;

    const fileParameter: FileParameter = {
      fileName: this.file.name,
      data: this.file,
    };

    this.apiSubscription = this.api
      .saveUserPicture(
        this.userId,
        fileParameter,
        scaleRatio,
        Math.round(this.panX / scaleRatio),
        Math.round(this.panY / scaleRatio)
        // this.viewPort
      )
      .subscribe((_) => {
        this.fileVersion++;
        this.closeModal();
      });
  }
}
