// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/** Error thrown when an HTTP request fails. */
export class HttpError extends Error {
    // @ts-ignore: Intentionally unused.
    // tslint:disable-next-line:variable-name
    private __proto__: Error;

    /** The HTTP status code represented by this error. */
    statusCode: number;

    /** Constructs a new instance of {@link @microsoft/signalr.HttpError}.
     *
     * @param {string} errorMessage A descriptive error message.
     * @param {number} statusCode The HTTP status code represented by this error.
     */
    constructor(errorMessage: string, statusCode: number) {
        const trueProto = new.target.prototype;
        super(errorMessage);
        this.statusCode = statusCode;

        // Workaround issue in Typescript compiler
        // https://github.com/Microsoft/TypeScript/issues/13965#issuecomment-278570200
        this.__proto__ = trueProto;
    }
}

/** Error thrown when a timeout elapses. */
export class TimeoutError extends Error {
    // @ts-ignore: Intentionally unused.
    // tslint:disable-next-line:variable-name
    private __proto__: Error;

    /** Constructs a new instance of {@link @microsoft/signalr.TimeoutError}.
     *
     * @param {string} errorMessage A descriptive error message.
     */
    constructor(errorMessage: string = "A timeout occurred.") {
        const trueProto = new.target.prototype;
        super(errorMessage);

        // Workaround issue in Typescript compiler
        // https://github.com/Microsoft/TypeScript/issues/13965#issuecomment-278570200
        this.__proto__ = trueProto;
    }
}

/** Error thrown when an action is aborted. */
export class AbortError extends Error {
    // @ts-ignore: Intentionally unused.
    // tslint:disable-next-line:variable-name
    private __proto__: Error;

    /** Constructs a new instance of {@link AbortError}.
     *
     * @param {string} errorMessage A descriptive error message.
     */
    constructor(errorMessage: string = "An abort occurred.") {
        const trueProto = new.target.prototype;
        super(errorMessage);

        // Workaround issue in Typescript compiler
        // https://github.com/Microsoft/TypeScript/issues/13965#issuecomment-278570200
        this.__proto__ = trueProto;
    }
}